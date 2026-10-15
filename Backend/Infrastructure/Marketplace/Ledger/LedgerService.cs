using Application.Common;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Ledger;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Ledger;

public class LedgerService : ILedgerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<LedgerService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task PostTransactionAsync(
        LedgerTransactionType type,
        string refType,
        Guid refId,
        string idempotencyKey,
        string memo,
        List<LedgerPostingLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines == null || lines.Count < 2)
            throw new ArgumentException("Một giao dịch sổ cái phải có ít nhất 2 bút toán (Nợ và Có).", nameof(lines));

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException($"Bút toán sổ cái không cân: Tổng Nợ ({totalDebit}) != Tổng Có ({totalCredit}).");
        }

        if (totalDebit <= 0)
        {
            throw new InvalidOperationException("Tổng số tiền của giao dịch sổ cái phải lớn hơn 0.");
        }

        // Kiểm tra Idempotency - nếu đã xử lý giao dịch này rồi thì an toàn bỏ qua
        if (await _unitOfWork.Ledgers.TransactionExistsAsync(idempotencyKey, cancellationToken))
        {
            _logger.LogWarning("Bút toán sổ cái với IdempotencyKey '{Key}' đã tồn tại. Bỏ qua ghi đúp.", idempotencyKey);
            return;
        }

        var transaction = new LedgerTransaction(type, refType, refId, idempotencyKey, memo);
        foreach (var line in lines)
        {
            transaction.AddEntry(new LedgerEntry(transaction.Id, line.AccountId, line.Debit, line.Credit));
        }

        await _unitOfWork.Ledgers.AddTransactionAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã ghi sổ thành công giao dịch {Type} cho {RefType}:{RefId} với tổng giá trị {Amount} VND.",
            type, refType, refId, totalDebit);
    }

    public async Task SettleCheckoutPaymentAsync(
        Guid checkoutId,
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        var idempotencyKey = $"settle:checkout:{checkoutId}";
        if (await _unitOfWork.Ledgers.TransactionExistsAsync(idempotencyKey, cancellationToken))
        {
            _logger.LogWarning("Đơn hàng checkout {CheckoutId} đã được phân bổ sổ cái trước đó.", checkoutId);
            return;
        }

        var orders = await _unitOfWork.Orders.GetOrdersByCheckoutIdAsync(checkoutId, cancellationToken);
        if (orders.Count == 0)
        {
            _logger.LogWarning("Không tìm thấy ShopOrder nào cho Checkout {CheckoutId} để phân bổ tiền.", checkoutId);
            return;
        }

        var unallocatedAccount = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.UnallocatedPayments, cancellationToken);
        var platformRevenueAccount = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.PlatformRevenue, cancellationToken);

        // Ghi nhận phân bổ doanh thu tách bạch cho từng đơn hàng (Revenue split recorded per order)
        foreach (var order in orders)
        {
            var orderIdempotencyKey = $"settle:order:{order.Id}";
            if (await _unitOfWork.Ledgers.TransactionExistsAsync(orderIdempotencyKey, cancellationToken))
            {
                continue;
            }

            var shopPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(order.ShopId, LedgerAccountType.ShopPending, cancellationToken);
            var shopNet = order.Items.Sum(i => i.ShopNet);
            var shopCommission = order.Items.Sum(i => i.CommissionAmount);

            var orderPostingLines = new List<LedgerPostingLine>
            {
                // Nợ UnallocatedPayments = Tổng giá trị đơn của Shop này
                new()
                {
                    AccountId = unallocatedAccount.Id,
                    Debit = order.Total,
                    Credit = 0
                },
                // Có ShopPending = Tiền ròng Shop nhận (tạm giữ)
                new()
                {
                    AccountId = shopPendingAccount.Id,
                    Debit = 0,
                    Credit = shopNet
                }
            };

            if (shopCommission > 0)
            {
                // Có PlatformRevenue = Hoa hồng sàn thu trên đơn này
                orderPostingLines.Add(new LedgerPostingLine
                {
                    AccountId = platformRevenueAccount.Id,
                    Debit = 0,
                    Credit = shopCommission
                });
            }

            await PostTransactionAsync(
                LedgerTransactionType.OrderSettlement,
                "ShopOrder",
                order.Id,
                orderIdempotencyKey,
                $"Phân bổ doanh thu đơn hàng #{order.OrderNo}: Tổng {order.Total:N0} đ (Shop nhận: {shopNet:N0} đ, Hoa hồng sàn: {shopCommission:N0} đ)",
                orderPostingLines,
                cancellationToken);

            // Cập nhật số dư cache của Shop
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(order.ShopId, cancellationToken);
            balanceCache.ApplyOrderSettlement(order.Total, shopCommission, shopNet);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

            // Invalidate Memory Cache số dư của Shop
            _cacheService.Remove($"shop:{order.ShopId}:balance");
        }
    }

    public async Task ReleaseOrderFundsAsync(
        Guid shopOrderId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyKey = $"release:order:{shopOrderId}";
        if (await _unitOfWork.Ledgers.TransactionExistsAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        var order = await _unitOfWork.Orders.GetByIdAsync(shopOrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Không tìm thấy ShopOrder {ShopOrderId} để giải phóng tiền.", shopOrderId);
            return;
        }

        var shopNet = order.Items.Sum(i => i.ShopNet);
        if (shopNet <= 0) return;

        var shopPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(order.ShopId, LedgerAccountType.ShopPending, cancellationToken);
        var shopAvailableAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(order.ShopId, LedgerAccountType.ShopAvailable, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            // Nợ ShopPending (giảm pending)
            new()
            {
                AccountId = shopPendingAccount.Id,
                Debit = shopNet,
                Credit = 0
            },
            // Có ShopAvailable (tăng khả dụng)
            new()
            {
                AccountId = shopAvailableAccount.Id,
                Debit = 0,
                Credit = shopNet
            }
        };

        var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(order.ShopId, cancellationToken);
        balanceCache.ReleasePendingToAvailable(shopNet);
        _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

        // Invalidate Memory Cache số dư của Shop
        _cacheService.Remove($"shop:{order.ShopId}:balance");

        await PostTransactionAsync(
            LedgerTransactionType.FundRelease,
            "ShopOrder",
            shopOrderId,
            idempotencyKey,
            $"Giải phóng tiền tạm giữ sang số dư khả dụng cho đơn {order.OrderNo}",
            lines,
            cancellationToken);
    }

    public async Task<ShopBalanceDto> GetShopBalanceAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
        {
            throw new InvalidOperationException("Người dùng chưa mở gian hàng.");
        }

        // Bọc kết quả trong Memory Cache (TTL 60s) để giảm tải DB khi người bán refresh liên tục
        var cacheKey = $"shop:{shop.Id}:balance";
        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                // Tự động kiểm tra và hạch toán doanh thu cho mọi đơn hàng chưa vào sổ cái
                await EnsureShopOrdersSettledAsync(shop.Id, ct);

                var cache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(shop.Id, ct);
                return new ShopBalanceDto
                {
                    ShopId = shop.Id,
                    PendingBalance = cache.PendingBalance,
                    AvailableBalance = cache.AvailableBalance,
                    PayoutPendingBalance = cache.PayoutPendingBalance,
                    LifetimeGrossRevenue = cache.LifetimeGrossRevenue,
                    LifetimeCommissionPaid = cache.LifetimeCommissionPaid,
                    LastUpdatedAt = cache.UpdatedAtUtc
                };
            },
            TimeSpan.FromSeconds(60),
            cancellationToken);
    }

    private async Task EnsureShopOrdersSettledAsync(Guid shopId, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _unitOfWork.Orders.GetShopOrdersAsync(shopId, null, 1, 100, cancellationToken);
            if (orders == null || orders.Count == 0) return;

            var unsettledOrders = new List<Domain.Entities.Marketplace.Orders.ShopOrder>();
            foreach (var order in orders)
            {
                if (order.Status is OrderStatus.Delivered or OrderStatus.Completed)
                {
                    var orderIdempotencyKey = $"settle:order:{order.Id}";
                    var exists = await _unitOfWork.Ledgers.TransactionExistsAsync(orderIdempotencyKey, cancellationToken);
                    if (!exists)
                    {
                        unsettledOrders.Add(order);
                    }
                }
            }

            if (unsettledOrders.Count == 0) return;

            _logger.LogInformation("Tự động đồng bộ hạch toán cho {Count} đơn hàng của Shop {ShopId}...",
                unsettledOrders.Count, shopId);

            var unallocatedAccount = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.UnallocatedPayments, cancellationToken);
            var platformRevenueAccount = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.PlatformRevenue, cancellationToken);
            var shopPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(shopId, LedgerAccountType.ShopPending, cancellationToken);
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(shopId, cancellationToken);

            foreach (var order in unsettledOrders)
            {
                var orderIdempotencyKey = $"settle:order:{order.Id}";
                var shopNet = order.Items.Sum(i => i.ShopNet);
                var shopCommission = order.Items.Sum(i => i.CommissionAmount);

                var lines = new List<LedgerPostingLine>
                {
                    new()
                    {
                        AccountId = unallocatedAccount.Id,
                        Debit = order.Total,
                        Credit = 0
                    },
                    new()
                    {
                        AccountId = shopPendingAccount.Id,
                        Debit = 0,
                        Credit = shopNet
                    }
                };

                if (shopCommission > 0)
                {
                    lines.Add(new LedgerPostingLine
                    {
                        AccountId = platformRevenueAccount.Id,
                        Debit = 0,
                        Credit = shopCommission
                    });
                }

                await PostTransactionAsync(
                    LedgerTransactionType.OrderSettlement,
                    "ShopOrder",
                    order.Id,
                    orderIdempotencyKey,
                    $"Tự động hạch toán doanh thu đơn hàng #{order.OrderNo}: Tổng {order.Total:N0} đ",
                    lines,
                    cancellationToken);

                balanceCache.ApplyOrderSettlement(order.Total, shopCommission, shopNet);
            }

            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình tự động đồng bộ doanh thu đơn hàng cho Shop {ShopId}", shopId);
        }
    }

    public async Task<PagedResult<LedgerStatementItemDto>> GetShopStatementAsync(
        Guid sellerUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
        {
            throw new InvalidOperationException("Người dùng chưa mở gian hàng.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _unitOfWork.Ledgers.GetShopStatementAsync(
            shop.Id,
            fromDate,
            toDate,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(e => new LedgerStatementItemDto
        {
            EntryId = e.Id,
            TransactionId = e.TransactionId,
            TransactionType = e.Transaction?.Type ?? LedgerTransactionType.PaymentReceived,
            AccountCode = e.Account?.Code ?? string.Empty,
            AccountName = e.Account?.Name ?? string.Empty,
            Debit = e.Debit,
            Credit = e.Credit,
            Memo = e.Transaction?.Memo ?? string.Empty,
            RefType = e.Transaction?.RefType ?? string.Empty,
            RefId = e.Transaction?.RefId ?? Guid.Empty,
            CreatedAt = e.CreatedAtUtc
        }).ToList();

        return new PagedResult<LedgerStatementItemDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
