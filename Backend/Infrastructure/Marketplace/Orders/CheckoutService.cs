using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.Interfaces;
using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Marketplace.Orders;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.External.Realtime;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Orders;

public sealed class CheckoutService : ICheckoutService
{
    private readonly SocialDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILedgerService _ledgerService;
    private readonly IHubContext<SocialHub> _hubContext;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        SocialDbContext context,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILedgerService ledgerService,
        IHubContext<SocialHub> hubContext,
        ILogger<CheckoutService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _ledgerService = ledgerService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<CheckoutDetailDto> CreateCheckoutAsync(
        Guid userId,
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra Idempotency chống bấm đúp
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingCheckout = await _unitOfWork.Checkouts.GetByIdempotencyKeyAsync(userId, request.IdempotencyKey.Trim(), cancellationToken);
            if (existingCheckout != null && existingCheckout.Status == CheckoutStatus.Open)
            {
                return MapToDetailDto(existingCheckout);
            }
        }

        // 2. Xác định danh sách các món cần thanh toán
        var itemsToCheckout = new List<(Guid VariantId, int Quantity)>();

        if (request.BuyNowItem != null)
        {
            itemsToCheckout.Add((request.BuyNowItem.VariantId, request.BuyNowItem.Quantity));
        }
        else if (request.Items != null && request.Items.Count > 0)
        {
            itemsToCheckout = request.Items.Select(i => (i.VariantId, i.Quantity)).ToList();
        }
        else
        {
            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken);
            if (cart == null || cart.Items.Count == 0)
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống.");
            }

            var candidateItems = cart.Items.AsEnumerable();
            if (request.SelectedVariantIds != null && request.SelectedVariantIds.Count > 0)
            {
                var selectedSet = request.SelectedVariantIds.ToHashSet();
                candidateItems = candidateItems.Where(i => selectedSet.Contains(i.VariantId));
            }

            itemsToCheckout = candidateItems.Select(i => (i.VariantId, i.Quantity)).ToList();
        }

        // Gộp các dòng trùng VariantId và lọc số lượng hợp lệ (> 0)
        itemsToCheckout = itemsToCheckout
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.VariantId)
            .Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .ToList();

        if (itemsToCheckout.Count == 0)
        {
            throw new InvalidOperationException("Không có sản phẩm hợp lệ nào được chọn để thanh toán.");
        }

        // 3. Thực hiện tạo Checkout và giữ mã trong Database Transaction
        var nowUtc = DateTime.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(15);
        var checkoutNo = $"CK{nowUtc:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var checkoutId = Guid.NewGuid();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var checkoutItems = new List<CheckoutItem>();
            long itemsTotal = 0;

            // Sắp xếp theo VariantId tăng dần để chống Deadlock khi nhiều giao dịch chạy song song
            var orderedItems = itemsToCheckout.OrderBy(i => i.VariantId).ToList();

            foreach (var (variantId, quantity) in orderedItems)
            {
                var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId, cancellationToken);
                if (variant == null || variant.DeletedAtUtc.HasValue)
                {
                    throw new InvalidOperationException("Một trong các biến thể sản phẩm đã bị xóa hoặc không tồn tại.");
                }

                var product = await _unitOfWork.Products.GetByIdAsync(variant.ProductId, cancellationToken);
                if (product == null || product.Status != ProductStatus.Listed || product.DeletedAtUtc.HasValue || product.HasActiveTakedown())
                {
                    throw new InvalidOperationException($"Sản phẩm '{product?.Name ?? "N/A"}' hiện không khả dụng để mua.");
                }

                var shop = await _unitOfWork.Shops.GetByIdAsync(variant.ShopId, cancellationToken);
                if (shop == null || shop.DeletedAtUtc.HasValue ||
                    shop.ApprovalStatus != ApprovalStatus.Approved ||
                    shop.OperatingStatus != OperatingStatus.Open ||
                    shop.Sanctions.Any(sn => sn.Type != ShopSanctionType.Warning && sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
                {
                    throw new InvalidOperationException($"Gian hàng '{shop?.Name ?? "N/A"}' hiện đang tạm đóng hoặc bị xử phạt.");
                }

                if (shop.OwnerUserId == userId)
                {
                    throw new InvalidOperationException($"Bạn không thể mua sản phẩm của chính gian hàng mình ('{product.Name}').");
                }

                // Tính toán tài chính dòng hàng
                var unitPrice = variant.Price;
                var lineTotal = unitPrice * quantity;
                itemsTotal += lineTotal;

                // Tỷ lệ hoa hồng (mặc định 10% = 1000 Bp nếu danh mục chưa cấu hình)
                var commissionRateBp = product.Category?.CommissionRateBp ?? 1000;
                var commissionAmount = (long)Math.Round((double)lineTotal * commissionRateBp / 10000.0);
                var shopNet = lineTotal - commissionAmount;

                var labelParts = variant.OptionValues.Select(ov => ov.OptionValue?.Value).Where(v => !string.IsNullOrEmpty(v));
                var variantLabel = labelParts.Any() ? string.Join(" / ", labelParts) : "Mặc định";

                var checkoutItem = new CheckoutItem(
                    checkoutId,
                    shop.Id,
                    variant.Id,
                    product.Id,
                    product.CategoryId,
                    quantity,
                    unitPrice,
                    lineTotal,
                    product.Name,
                    variantLabel,
                    variant.Sku,
                    commissionRateBp,
                    commissionAmount,
                    shopNet,
                    variant.DeliveryType);

                checkoutItems.Add(checkoutItem);

                // 4. Khóa giữ mã kho đồng thời (FOR UPDATE SKIP LOCKED)
                if (RequiresCodeReservation(variant))
                {
                    var affected = await ReserveCodesAsync(checkoutId, variant.Id, quantity, nowUtc, cancellationToken);

                    if (affected < quantity)
                    {
                        throw new InvalidOperationException($"Biến thể '{product.Name} - {variantLabel}' không đủ số lượng trong kho (chỉ còn {affected} mã khả dụng).");
                    }
                }
            }

            var checkout = new Checkout(
                checkoutNo,
                userId,
                itemsTotal,
                0L,
                itemsTotal,
                expiresAtUtc,
                request.IdempotencyKey);

            // Gán Id thủ công để khớp với checkoutId đã cập nhật vào DigitalCodes
            typeof(Domain.Common.BaseEntity).GetProperty("Id")?.SetValue(checkout, checkoutId);

            foreach (var item in checkoutItems)
            {
                checkout.Items.Add(item);
            }

            await _unitOfWork.Checkouts.AddAsync(checkout, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Đã khởi tạo Checkout {CheckoutNo} cho User {UserId} với tổng tiền {GrandTotal} VND", checkoutNo, userId, itemsTotal);
            return MapToDetailDto(checkout);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CheckoutDetailDto?> GetCheckoutByIdAsync(Guid userId, Guid checkoutId, CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(checkoutId, cancellationToken);
        if (checkout == null || checkout.BuyerUserId != userId)
        {
            return null;
        }

        // Tự động kiểm tra hết hạn nếu đang Open mà đã quá hạn
        if (checkout.Status == CheckoutStatus.Open && checkout.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await ExpireCheckoutAsync(checkout.Id, cancellationToken);
            // ExpireCheckoutAsync cập nhật trực tiếp dưới DB -> nạp lại entity đang được track
            await _context.Entry(checkout).ReloadAsync(cancellationToken);
        }

        return checkout != null ? MapToDetailDto(checkout) : null;
    }

    public async Task<CheckoutPaymentResult> CompleteCheckoutPaymentAsync(
        Guid checkoutId,
        string providerTxnId,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(checkoutId, cancellationToken);
        if (checkout == null)
        {
            throw new KeyNotFoundException("Không tìm thấy phiên Checkout.");
        }

        // Luôn đọc trạng thái mới nhất trực tiếp từ DB (entity có thể đã được track từ trước trong cùng scope)
        var initialStatus = await GetCurrentStatusAsync(checkoutId, cancellationToken);
        if (initialStatus is CheckoutStatus.Paid or CheckoutStatus.PaidAfterExpiry)
        {
            // Idempotent: đã xử lý rồi thì trả về kết quả trước đó
            return await BuildPaymentResultFromCurrentStateAsync(checkoutId, cancellationToken);
        }

        var createdOrderIds = new List<Guid>();
        var nowUtc = DateTime.UtcNow;
        var complaintWindowEndsAtUtc = nowUtc.AddHours(48);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Giành quyền xử lý: chỉ đúng 1 request đổi được Open -> Paid.
            //    Request song song sẽ chờ khóa dòng rồi nhận 0 dòng -> không sinh đơn trùng.
            var claimed = await TryChangeStatusAsync(checkoutId, CheckoutStatus.Open, CheckoutStatus.Paid, nowUtc, cancellationToken);

            if (!claimed)
            {
                var statusNow = await GetCurrentStatusAsync(checkoutId, cancellationToken);
                var isLatePayment = statusNow is CheckoutStatus.Expired or CheckoutStatus.Failed;

                // Khóa dòng Checkout ở trạng thái hiện tại để các request thanh toán muộn khác phải xếp hàng chờ
                if (!isLatePayment || !await TryChangeStatusAsync(checkoutId, statusNow, statusNow, nowUtc, cancellationToken))
                {
                    // Một request khác đã (hoặc vừa) xử lý xong phiên này
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return await BuildPaymentResultFromCurrentStateAsync(checkoutId, cancellationToken);
                }

                // 2. Tiền về sau khi phiên đã hết hạn: thử giữ lại mã trong kho
                if (await TryReReserveCodesAsync(checkout, nowUtc, cancellationToken))
                {
                    await TryChangeStatusAsync(checkoutId, statusNow, CheckoutStatus.Paid, nowUtc, cancellationToken);
                    _logger.LogWarning("Checkout {CheckoutId} được thanh toán sau khi hết hạn nhưng kho vẫn đủ mã -> tiếp tục giao hàng.", checkoutId);
                }
                else
                {
                    // 3. Kho không còn đủ mã: trả lại các mã vừa giữ được (nếu có) và chuyển sang chờ hoàn tiền
                    await ReleaseReservedCodesAsync(checkoutId, nowUtc, cancellationToken);
                    await TryChangeStatusAsync(checkoutId, statusNow, CheckoutStatus.PaidAfterExpiry, nowUtc, cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogWarning("Checkout {CheckoutId} nhận tiền sau khi hết hạn và kho đã hết mã (TxnId: {TxnId}) -> PaidAfterExpiry, chờ hoàn tiền.", checkoutId, providerTxnId);
                    await NotifyLatePaymentRefundAsync(checkout, cancellationToken);

                    return new CheckoutPaymentResult(RefundRequired: true, OrderIds: new List<Guid>());
                }
            }

            // 4. Đã giành được quyền xử lý -> sinh đơn hàng và giao mã.
            //    Lưu ý: KHÔNG gọi _unitOfWork.Checkouts.Update(checkout) vì entity đang giữ trạng thái cũ,
            //    sẽ ghi đè trạng thái Paid vừa cập nhật nguyên tử.

            // Nhóm các món theo từng Shop (1 Checkout có thể mua từ nhiều Shop -> sinh nhiều ShopOrder)
            var shopGroups = checkout.Items.GroupBy(i => i.ShopId);

            foreach (var group in shopGroups)
            {
                var shopId = group.Key;
                var shopItems = group.ToList();

                var orderNo = $"ORD{nowUtc:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                var shopGross = shopItems.Sum(i => i.LineTotal);
                var commissionTotal = shopItems.Sum(i => i.CommissionAmount);
                var shopNet = shopGross - commissionTotal;

                var shopOrder = new ShopOrder(
                    orderNo,
                    checkout.Id,
                    shopId,
                    checkout.BuyerUserId,
                    shopGross,
                    shopGross,
                    commissionTotal,
                    shopNet,
                    nowUtc,
                    complaintWindowEndsAtUtc);

                var fulfillment = new Fulfillment(shopOrder.Id, FulfillmentStatus.Fulfilled);

                foreach (var cItem in shopItems)
                {
                    var orderItem = new OrderItem(
                        shopOrder.Id,
                        cItem.VariantId,
                        cItem.ProductId,
                        cItem.CategoryId,
                        cItem.ProductNameSnapshot,
                        cItem.VariantLabelSnapshot,
                        cItem.SkuSnapshot,
                        cItem.UnitPrice,
                        cItem.Quantity,
                        cItem.LineTotal,
                        cItem.LineTotal,
                        cItem.CommissionRateBp,
                        cItem.CommissionAmount,
                        cItem.ShopNet,
                        cItem.DeliveryType);

                    shopOrder.Items.Add(orderItem);

                    // 1. Chuyển mọi mã đang giữ cho phiên này: Reserved -> Delivered và gắn OrderItemId.
                    //    Không lọc theo DeliveryType: mã nào đã được giữ (theo RequiresCodeReservation) đều phải được giao,
                    //    tránh mã bị kẹt vĩnh viễn ở trạng thái Reserved. Biến thể không giữ mã -> cập nhật 0 dòng.
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE ""DigitalCodes""
                        SET ""Status"" = 'Delivered',
                            ""OrderItemId"" = {orderItem.Id},
                            ""DeliveredAtUtc"" = {nowUtc},
                            ""ReservedByCheckoutId"" = NULL,
                            ""UpdatedAtUtc"" = {nowUtc}
                        WHERE ""ReservedByCheckoutId"" = {checkout.Id}
                          AND ""VariantId"" = {cItem.VariantId}
                          AND ""Status"" = 'Reserved'
                    ", cancellationToken);

                    // 2. Tạo dòng FulfillmentItem
                    var fItem = new FulfillmentItem(fulfillment.Id, orderItem.Id, cItem.Quantity);
                    fulfillment.Items.Add(fItem);
                }

                shopOrder.Fulfillments.Add(fulfillment);

                // Lịch sử trạng thái
                var history = new OrderStatusHistory(
                    shopOrder.Id,
                    null,
                    OrderStatus.Delivered,
                    OrderActorType.System,
                    null,
                    $"Thanh toán thành công. Mã giao dịch: {providerTxnId}");

                shopOrder.Histories.Add(history);

                await _unitOfWork.Orders.AddAsync(shopOrder, cancellationToken);
                createdOrderIds.Add(shopOrder.Id);
            }

            // Xóa các món đã mua khỏi giỏ hàng của người mua
            var cart = await _unitOfWork.Carts.GetByUserIdAsync(checkout.BuyerUserId, cancellationToken);
            if (cart != null)
            {
                var boughtVariantIds = checkout.Items.Select(i => i.VariantId).ToList();
                await _unitOfWork.Carts.RemoveItemsByVariantIdsAsync(cart.Id, boughtVariantIds, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Thanh toán thành công Checkout {CheckoutId}. Đã sinh {OrderCount} đơn hàng", checkout.Id, createdOrderIds.Count);

            // Gửi thông báo cho người mua
            try
            {
                await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
                {
                    UserId = checkout.BuyerUserId,
                    Type = "OrderPaid",
                    Title = "Thanh toán thành công!",
                    Body = $"Đơn hàng của bạn đã được thanh toán và giao mã số thành công. Mã phiên: {checkout.CheckoutNo}"
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gửi thông báo thanh toán cho User {UserId}", checkout.BuyerUserId);
            }

            return new CheckoutPaymentResult(RefundRequired: false, OrderIds: createdOrderIds);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MockPayResultDto> MockPayAsync(
        Guid userId,
        Guid checkoutId,
        MockPayRequest request,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(checkoutId, cancellationToken);
        if (checkout == null || checkout.BuyerUserId != userId)
        {
            throw new KeyNotFoundException("Không tìm thấy phiên thanh toán hợp lệ.");
        }

        if (string.Equals(request.Result, "success", StringComparison.OrdinalIgnoreCase))
        {
            // Thanh toán giả lập không có tiền thật -> không áp dụng luồng "tiền về muộn / hoàn tiền"
            if (checkout.Status is CheckoutStatus.Expired or CheckoutStatus.Failed or CheckoutStatus.PaidAfterExpiry)
            {
                throw new InvalidOperationException($"Phiên Checkout không thể thanh toán do đang ở trạng thái '{checkout.Status}'.");
            }

            var mockTxnId = $"MOCK-PAY-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var result = await CompleteCheckoutPaymentAsync(checkoutId, mockTxnId, cancellationToken);

            // Lưu bản ghi Payment và hạch toán doanh thu vào Sổ cái cho các Shop
            try
            {
                var existingPayment = await _unitOfWork.Payments.GetByCheckoutIdAsync(checkoutId, cancellationToken);
                Domain.Entities.Marketplace.Payments.Payment payment;

                if (existingPayment != null)
                {
                    existingPayment.MarkSucceeded(DateTime.UtcNow, "Thanh toán giả lập thành công");
                    _unitOfWork.Payments.Update(existingPayment);
                    payment = existingPayment;
                }
                else
                {
                    payment = new Domain.Entities.Marketplace.Payments.Payment(
                        checkoutId,
                        PaymentProvider.Mock,
                        mockTxnId,
                        checkout.GrandTotal,
                        "MockQr",
                        "VND");
                    payment.MarkSucceeded(DateTime.UtcNow, "Thanh toán giả lập thành công");
                    await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                try
                {
                    var gatewayClearing = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.GatewayClearing, cancellationToken);
                    var unallocated = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.UnallocatedPayments, cancellationToken);

                    var ledgerLinesA = new List<Application.DTOs.Marketplace.Ledger.LedgerPostingLine>
                    {
                        new() { AccountId = gatewayClearing.Id, Debit = payment.Amount, Credit = 0 },
                        new() { AccountId = unallocated.Id, Debit = 0, Credit = payment.Amount }
                    };

                    await _ledgerService.PostTransactionAsync(
                        LedgerTransactionType.PaymentReceived,
                        "Payment",
                        payment.Id,
                        $"pay_recv:{payment.Id}",
                        $"Nhận tiền thanh toán giả lập {payment.ProviderTxnId}",
                        ledgerLinesA,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi ghi sổ nhận tiền ban đầu cho phiên giả lập {CheckoutId}", checkoutId);
                }

                await _ledgerService.SettleCheckoutPaymentAsync(checkoutId, payment, cancellationToken);

                // Phát SignalR thông báo thanh toán thành công tới client đang mở trang Checkout
                try
                {
                    await _hubContext.Clients.Group($"checkout_{checkoutId}").SendAsync("PaymentSucceeded", new
                    {
                        checkoutId = checkoutId,
                        paymentId = payment.Id,
                        amount = payment.Amount,
                        status = "Succeeded",
                        paidAt = DateTime.UtcNow
                    }, cancellationToken);

                    _logger.LogInformation("MockPay đã phát SignalR PaymentSucceeded tới group checkout_{CheckoutId}", checkoutId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi phát SignalR PaymentSucceeded từ MockPay cho Checkout {CheckoutId}", checkoutId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hạch toán sổ cái cho phiên thanh toán giả lập {CheckoutId}", checkoutId);
            }

            return new MockPayResultDto
            {
                CheckoutId = checkoutId,
                CheckoutStatus = "Paid",
                OrderIds = result.OrderIds,
                Message = "Thanh toán giả lập thành công! Hàng số đã được xuất giao vào mục Đơn hàng của bạn."
            };
        }
        else
        {
            await ExpireCheckoutAsync(checkoutId, cancellationToken);

            return new MockPayResultDto
            {
                CheckoutId = checkoutId,
                CheckoutStatus = "Failed",
                OrderIds = new List<Guid>(),
                Message = "Thanh toán giả lập thất bại. Mã giữ chỗ đã được giải phóng lại kho."
            };
        }
    }

    public async Task ExpireCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Chỉ hết hạn khi phiên vẫn còn Open. Nếu đúng lúc đó tiền về và phiên vừa chuyển Paid,
            // câu UPDATE này nhận 0 dòng -> KHÔNG giải phóng mã của đơn đã thanh toán.
            var claimed = await TryChangeStatusAsync(checkoutId, CheckoutStatus.Open, CheckoutStatus.Expired, nowUtc, cancellationToken);
            if (!claimed)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            // Giải phóng các mã Reserved quay lại trạng thái Available
            await ReleaseReservedCodesAsync(checkoutId, nowUtc, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Đã hủy và giải phóng mã giữ chỗ cho Checkout quá hạn {CheckoutId}", checkoutId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    #region Concurrency helpers

    /// <summary>
    /// Quy tắc DUY NHẤT quyết định biến thể có cần giữ mã trong kho hay không.
    /// Dùng chung cho lúc tạo Checkout và lúc giữ lại mã khi tiền về muộn.
    /// </summary>
    private static bool RequiresCodeReservation(ProductVariant variant)
        => variant.DeliveryType == DeliveryType.CodePool || variant.TrackInventory;

    /// <summary>
    /// Giữ tối đa <paramref name="quantity"/> mã Available cho phiên. FOR UPDATE SKIP LOCKED giúp
    /// các giao dịch song song không bao giờ lấy trùng một mã. Trả về số mã giữ được.
    /// </summary>
    private Task<int> ReserveCodesAsync(Guid checkoutId, Guid variantId, int quantity, DateTime nowUtc, CancellationToken cancellationToken)
    {
        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""DigitalCodes""
            SET ""Status"" = 'Reserved',
                ""ReservedByCheckoutId"" = {checkoutId},
                ""UpdatedAtUtc"" = {nowUtc}
            WHERE ""Id"" IN (
                SELECT ""Id"" FROM ""DigitalCodes""
                WHERE ""VariantId"" = {variantId} AND ""Status"" = 'Available'
                ORDER BY ""Id""
                LIMIT {quantity}
                FOR UPDATE SKIP LOCKED
            )", cancellationToken);
    }

    /// <summary>
    /// Trả mọi mã đang giữ của phiên về lại trạng thái Available.
    /// </summary>
    private Task<int> ReleaseReservedCodesAsync(Guid checkoutId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""DigitalCodes""
            SET ""Status"" = 'Available',
                ""ReservedByCheckoutId"" = NULL,
                ""UpdatedAtUtc"" = {nowUtc}
            WHERE ""ReservedByCheckoutId"" = {checkoutId} AND ""Status"" = 'Reserved'
        ", cancellationToken);
    }

    /// <summary>
    /// Giữ lại mã cho toàn bộ phiên khi tiền về sau hạn. Trả về false nếu bất kỳ dòng nào không đủ mã
    /// (các mã đã giữ được sẽ do phía gọi trả lại bằng <see cref="ReleaseReservedCodesAsync"/>).
    /// </summary>
    private async Task<bool> TryReReserveCodesAsync(Checkout checkout, DateTime nowUtc, CancellationToken cancellationToken)
    {
        // Sắp xếp theo VariantId giống lúc tạo Checkout để chống deadlock
        foreach (var item in checkout.Items.OrderBy(i => i.VariantId))
        {
            var variant = item.Variant
                ?? await _unitOfWork.ProductVariants.GetByIdAsync(item.VariantId, cancellationToken);

            if (variant == null || !RequiresCodeReservation(variant))
            {
                continue;
            }

            var affected = await ReserveCodesAsync(checkout.Id, item.VariantId, item.Quantity, nowUtc, cancellationToken);
            if (affected < item.Quantity)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Đổi trạng thái Checkout một cách nguyên tử: UPDATE ... WHERE Status = from.
    /// Trả về true nếu đổi được (đúng 1 dòng). Khi from == to, câu lệnh chỉ có tác dụng khóa dòng.
    /// </summary>
    private async Task<bool> TryChangeStatusAsync(
        Guid checkoutId,
        CheckoutStatus from,
        CheckoutStatus to,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var query = _context.Checkouts.Where(c => c.Id == checkoutId && c.Status == from);
        var isPaidState = from != to && to is CheckoutStatus.Paid or CheckoutStatus.PaidAfterExpiry;

        var affected = isPaidState
            ? await query.ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, to)
                .SetProperty(c => c.PaidAtUtc, (DateTime?)nowUtc)
                .SetProperty(c => c.UpdatedAtUtc, nowUtc),
                cancellationToken)
            : await query.ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, to)
                .SetProperty(c => c.UpdatedAtUtc, nowUtc),
                cancellationToken);

        return affected == 1;
    }

    private Task<CheckoutStatus> GetCurrentStatusAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        return _context.Checkouts
            .AsNoTracking()
            .Where(c => c.Id == checkoutId)
            .Select(c => c.Status)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Dựng kết quả từ trạng thái đã được một request khác xử lý xong (dùng cho luồng idempotent).
    /// </summary>
    private async Task<CheckoutPaymentResult> BuildPaymentResultFromCurrentStateAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var status = await GetCurrentStatusAsync(checkoutId, cancellationToken);
        switch (status)
        {
            case CheckoutStatus.Paid:
                var orderIds = await _context.ShopOrders
                    .Where(o => o.CheckoutId == checkoutId)
                    .Select(o => o.Id)
                    .ToListAsync(cancellationToken);
                return new CheckoutPaymentResult(RefundRequired: false, OrderIds: orderIds);

            case CheckoutStatus.PaidAfterExpiry:
                return new CheckoutPaymentResult(RefundRequired: true, OrderIds: new List<Guid>());

            default:
                throw new InvalidOperationException($"Phiên Checkout không thể thanh toán do đang ở trạng thái '{status}'.");
        }
    }

    private async Task NotifyLatePaymentRefundAsync(Checkout checkout, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new Application.DTOs.Notifications.CreateNotificationDto
            {
                UserId = checkout.BuyerUserId,
                Type = "PaymentRefundPending",
                Title = "Thanh toán đến sau khi phiên hết hạn",
                Body = $"Chúng tôi đã nhận được thanh toán cho phiên {checkout.CheckoutNo} nhưng phiên đã hết hạn và sản phẩm không còn đủ hàng. Khoản tiền sẽ được hoàn lại cho bạn."
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi thông báo hoàn tiền cho User {UserId}", checkout.BuyerUserId);
        }
    }

    #endregion

    private static CheckoutDetailDto MapToDetailDto(Checkout checkout)
    {
        return new CheckoutDetailDto
        {
            Id = checkout.Id,
            CheckoutNo = checkout.CheckoutNo,
            Status = checkout.Status.ToString(),
            ItemsTotal = checkout.ItemsTotal,
            DiscountTotal = checkout.DiscountTotal,
            GrandTotal = checkout.GrandTotal,
            ExpiresAtUtc = checkout.ExpiresAtUtc,
            Items = checkout.Items.Select(i => new CheckoutItemDetailDto
            {
                Id = i.Id,
                ShopId = i.ShopId,
                ShopName = i.Shop?.Name ?? string.Empty,
                VariantId = i.VariantId,
                ProductId = i.ProductId,
                ProductName = i.ProductNameSnapshot,
                VariantLabel = i.VariantLabelSnapshot,
                Sku = i.SkuSnapshot,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal,
                DeliveryType = i.DeliveryType.ToString()
            }).ToList()
        };
    }
}
