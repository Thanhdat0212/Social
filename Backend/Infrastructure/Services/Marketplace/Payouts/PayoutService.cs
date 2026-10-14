using Application.Common;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Payouts;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Marketplace.Payouts;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Payouts;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Payouts;

public class PayoutService : IPayoutService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerService _ledgerService;
    private readonly ICryptoService _cryptoService;
    private readonly Application.Common.ICacheService _cacheService;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(
        IUnitOfWork unitOfWork,
        ILedgerService ledgerService,
        ICryptoService cryptoService,
        Application.Common.ICacheService cacheService,
        ILogger<PayoutService> logger)
    {
        _unitOfWork = unitOfWork;
        _ledgerService = ledgerService;
        _cryptoService = cryptoService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ShopBankAccountDto> SetBankAccountAsync(
        Guid sellerUserId,
        CreateShopBankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            throw new InvalidOperationException("Bạn chưa mở gian hàng trên hệ thống.");

        if (string.IsNullOrWhiteSpace(request.BankCode) ||
            string.IsNullOrWhiteSpace(request.BankName) ||
            string.IsNullOrWhiteSpace(request.AccountNumber) ||
            string.IsNullOrWhiteSpace(request.AccountHolderName))
        {
            throw new ArgumentException("Thông tin ngân hàng không được để trống.");
        }

        var encrypted = _cryptoService.Encrypt(request.AccountNumber.Trim());
        var masked = _cryptoService.MaskValue(request.AccountNumber.Trim());

        var existingAccount = await _unitOfWork.ShopBankAccounts.GetActiveByShopIdAsync(shop.Id, cancellationToken);
        if (existingAccount != null)
        {
            // Cập nhật thông tin tài khoản và kích hoạt khóa bảo mật 24h
            existingAccount.UpdateAccount(
                request.BankCode,
                request.BankName,
                encrypted,
                masked,
                request.AccountHolderName,
                lockHours: 24);
            _unitOfWork.ShopBankAccounts.Update(existingAccount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Shop {ShopId} đã cập nhật tài khoản ngân hàng. Khóa rút tiền đến {LockedUntil}.",
                shop.Id, existingAccount.LockedUntilUtc);

            return MapToBankAccountDto(existingAccount);
        }

        var newAccount = new ShopBankAccount(
            shop.Id,
            request.BankCode,
            request.BankName,
            encrypted,
            masked,
            request.AccountHolderName,
            lockHours: 24);

        await _unitOfWork.ShopBankAccounts.AddAsync(newAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop {ShopId} đã liên kết tài khoản ngân hàng mới. Khóa rút tiền đến {LockedUntil}.",
            shop.Id, newAccount.LockedUntilUtc);

        return MapToBankAccountDto(newAccount);
    }

    public async Task<ShopBankAccountDto?> GetBankAccountAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null) return null;

        var account = await _unitOfWork.ShopBankAccounts.GetActiveByShopIdAsync(shop.Id, cancellationToken);
        return account != null ? MapToBankAccountDto(account) : null;
    }

    public async Task<PayoutDto> RequestPayoutAsync(
        Guid sellerUserId,
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount < 100_000)
            throw new ArgumentException("Số tiền rút tối thiểu là 100.000 VNĐ.");

        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            throw new InvalidOperationException("Bạn chưa mở gian hàng trên hệ thống.");

        var bankAccount = await _unitOfWork.ShopBankAccounts.GetActiveByShopIdAsync(shop.Id, cancellationToken);
        if (bankAccount == null || bankAccount.Status != BankAccountStatus.Verified)
            throw new InvalidOperationException("Gian hàng chưa cài đặt tài khoản ngân hàng nhận tiền hoặc tài khoản chưa xác thực.");

        // Kiểm tra khóa bảo mật sau cập nhật tài khoản
        if (bankAccount.LockedUntilUtc.HasValue && bankAccount.LockedUntilUtc.Value > DateTime.UtcNow)
        {
            var timeLeft = bankAccount.LockedUntilUtc.Value - DateTime.UtcNow;
            throw new InvalidOperationException($"Tài khoản ngân hàng đang trong thời gian khóa bảo vệ (còn {Math.Ceiling(timeLeft.TotalHours)} giờ). Không thể thực hiện rút tiền lúc này.");
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.RequestIdempotencyKey)
            ? $"payout:{shop.Id}:{Guid.NewGuid():N}"
            : request.RequestIdempotencyKey.Trim();

        // Idempotency check
        var existingPayout = await _unitOfWork.Payouts.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingPayout != null)
        {
            return MapToPayoutDto(existingPayout);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Kiểm tra shop có đang có lệnh rút tiền đang mở không (Requested, Approved, Processing, Unknown)
            var hasActive = await _unitOfWork.Payouts.HasActivePayoutAsync(shop.Id, cancellationToken);
            if (hasActive)
            {
                throw new InvalidOperationException("Gian hàng đang có một lệnh rút tiền đang được xử lý. Vui lòng chờ lệnh đó hoàn tất trước khi tạo yêu cầu mới.");
            }

            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(shop.Id, cancellationToken);
            if (balanceCache.AvailableBalance < request.Amount)
            {
                throw new InvalidOperationException($"Số dư khả dụng ({balanceCache.AvailableBalance:N0} đ) không đủ để rút {request.Amount:N0} đ.");
            }

            // Phí rút tiền hiện tại mặc định = 0
            long fee = 0;
            var payout = new Payout(shop.Id, bankAccount.Id, request.Amount, fee, idempotencyKey);
            await _unitOfWork.Payouts.AddAsync(payout, cancellationToken);

            // Cập nhật số dư cache: Trừ AvailableBalance, Chuyển sang PayoutPendingBalance
            balanceCache.HoldPayout(request.Amount);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

            // Ghi nhận sổ cái kép: Nợ ShopAvailable / Có PayoutPending
            var shopAvailableAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(shop.Id, LedgerAccountType.ShopAvailable, cancellationToken);
            var shopPayoutPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(shop.Id, LedgerAccountType.PayoutPending, cancellationToken);

            var lines = new List<LedgerPostingLine>
            {
                new()
                {
                    AccountId = shopAvailableAccount.Id,
                    Debit = request.Amount,
                    Credit = 0
                },
                new()
                {
                    AccountId = shopPayoutPendingAccount.Id,
                    Debit = 0,
                    Credit = request.Amount
                }
            };

            await _ledgerService.PostTransactionAsync(
                LedgerTransactionType.PayoutRequested,
                "Payout",
                payout.Id,
                $"ledger:payout_req:{payout.Id}",
                $"Yêu cầu rút tiền #{payout.Id}: {request.Amount:N0} đ",
                lines,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _cacheService.Remove($"shop:{shop.Id}:balance");

            _logger.LogInformation("Shop {ShopId} đã tạo yêu cầu rút tiền #{PayoutId} thành công với số tiền {Amount} đ.",
                shop.Id, payout.Id, request.Amount);

            var createdPayout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payout.Id, cancellationToken);
            return MapToPayoutDto(createdPayout ?? payout);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<PayoutDto>> GetShopPayoutsAsync(
        Guid sellerUserId,
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            return new PagedResult<PayoutDto> { Items = new List<PayoutDto>(), TotalCount = 0, Page = page, PageSize = pageSize };

        var (items, total) = await _unitOfWork.Payouts.GetShopPayoutsAsync(shop.Id, status, page, pageSize, cancellationToken);
        var dtos = items.Select(MapToPayoutDto).ToList();

        return new PagedResult<PayoutDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PayoutDto> GetShopPayoutDetailAsync(
        Guid sellerUserId,
        Guid payoutId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            throw new InvalidOperationException("Không tìm thấy thông tin gian hàng.");

        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null || payout.ShopId != shop.Id)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền yêu cầu.");

        return MapToPayoutDto(payout);
    }

    public async Task<PayoutDto> CancelPayoutAsync(
        Guid sellerUserId,
        Guid payoutId,
        ShopCancelPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            throw new InvalidOperationException("Không tìm thấy thông tin gian hàng.");

        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null || payout.ShopId != shop.Id)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền yêu cầu.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            payout.CancelByShop(sellerUserId, request.Reason);
            _unitOfWork.Payouts.Update(payout);

            // Bút toán đảo: Hoàn tiền từ PayoutPending về lại ShopAvailable
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(shop.Id, cancellationToken);
            balanceCache.RevertPayout(payout.Amount);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

            var shopAvailableAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(shop.Id, LedgerAccountType.ShopAvailable, cancellationToken);
            var shopPayoutPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(shop.Id, LedgerAccountType.PayoutPending, cancellationToken);

            var lines = new List<LedgerPostingLine>
            {
                new()
                {
                    AccountId = shopPayoutPendingAccount.Id,
                    Debit = payout.Amount,
                    Credit = 0
                },
                new()
                {
                    AccountId = shopAvailableAccount.Id,
                    Debit = 0,
                    Credit = payout.Amount
                }
            };

            await _ledgerService.PostTransactionAsync(
                LedgerTransactionType.PayoutFailed,
                "Payout",
                payout.Id,
                $"ledger:payout_cancel:{payout.Id}",
                $"Hủy lệnh rút tiền #{payout.Id}: Hoàn lại {payout.Amount:N0} đ",
                lines,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _cacheService.Remove($"shop:{shop.Id}:balance");

            _logger.LogInformation("Shop {ShopId} đã tự hủy lệnh rút tiền #{PayoutId}.", shop.Id, payout.Id);
            return MapToPayoutDto(payout);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<PayoutDto>> GetAdminPayoutsAsync(
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _unitOfWork.Payouts.GetAdminPayoutsAsync(status, page, pageSize, cancellationToken);
        var dtos = items.Select(MapToPayoutDto).ToList();

        return new PagedResult<PayoutDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PayoutDto> GetAdminPayoutDetailAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default)
    {
        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền.");

        return MapToPayoutDto(payout);
    }

    public async Task<PayoutDto> ApprovePayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminApprovePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền.");

        payout.Approve(adminId, request.AdminNote);
        _unitOfWork.Payouts.Update(payout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} đã phê duyệt lệnh rút tiền #{PayoutId}.", adminId, payout.Id);
        return MapToPayoutDto(payout);
    }

    public async Task<PayoutDto> RejectPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminRejectPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Lý do từ chối không được để trống.");

        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            payout.Reject(adminId, request.Reason);
            _unitOfWork.Payouts.Update(payout);

            // Bút toán đảo: Hoàn tiền từ PayoutPending về lại ShopAvailable
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(payout.ShopId, cancellationToken);
            balanceCache.RevertPayout(payout.Amount);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

            var shopAvailableAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(payout.ShopId, LedgerAccountType.ShopAvailable, cancellationToken);
            var shopPayoutPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(payout.ShopId, LedgerAccountType.PayoutPending, cancellationToken);

            var lines = new List<LedgerPostingLine>
            {
                new()
                {
                    AccountId = shopPayoutPendingAccount.Id,
                    Debit = payout.Amount,
                    Credit = 0
                },
                new()
                {
                    AccountId = shopAvailableAccount.Id,
                    Debit = 0,
                    Credit = payout.Amount
                }
            };

            await _ledgerService.PostTransactionAsync(
                LedgerTransactionType.PayoutFailed,
                "Payout",
                payout.Id,
                $"ledger:payout_reject:{payout.Id}",
                $"Từ chối lệnh rút tiền #{payout.Id}: Hoàn lại {payout.Amount:N0} đ (Lý do: {request.Reason})",
                lines,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _cacheService.Remove($"shop:{payout.ShopId}:balance");

            _logger.LogInformation("Admin {AdminId} đã từ chối lệnh rút tiền #{PayoutId}. Lý do: {Reason}",
                adminId, payout.Id, request.Reason);

            return MapToPayoutDto(payout);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PayoutDto> MarkPaidPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminMarkPaidPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BankTransactionRef))
            throw new ArgumentException("Mã tham chiếu giao dịch ngân hàng không được để trống.");

        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            payout.MarkSucceeded(request.BankTransactionRef.Trim(), adminId, request.Note);
            _unitOfWork.Payouts.Update(payout);

            // Hoàn tất Payout: Trừ dứt điểm PayoutPendingBalance trong cache
            var balanceCache = await _unitOfWork.Ledgers.GetOrCreateBalanceCacheAsync(payout.ShopId, cancellationToken);
            balanceCache.CompletePayout(payout.Amount);
            _unitOfWork.Ledgers.UpdateBalanceCache(balanceCache);

            // Bút toán hoàn tất giải ngân: Nợ ShopPayoutPending / Có BankClearing hệ thống
            var shopPayoutPendingAccount = await _unitOfWork.Ledgers.GetOrCreateShopAccountAsync(payout.ShopId, LedgerAccountType.PayoutPending, cancellationToken);
            var bankClearingAccount = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.BankClearing, cancellationToken);

            var lines = new List<LedgerPostingLine>
            {
                new()
                {
                    AccountId = shopPayoutPendingAccount.Id,
                    Debit = payout.Amount,
                    Credit = 0
                },
                new()
                {
                    AccountId = bankClearingAccount.Id,
                    Debit = 0,
                    Credit = payout.Amount
                }
            };

            await _ledgerService.PostTransactionAsync(
                LedgerTransactionType.PayoutCompleted,
                "Payout",
                payout.Id,
                $"ledger:payout_success:{payout.Id}",
                $"Chi trả thành công lệnh rút tiền #{payout.Id}: {payout.Amount:N0} đ (Mã GD: {request.BankTransactionRef})",
                lines,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _cacheService.Remove($"shop:{payout.ShopId}:balance");

            _logger.LogInformation("Admin {AdminId} đã giải ngân thành công lệnh rút tiền #{PayoutId} (Mã GD: {Ref}).",
                adminId, payout.Id, request.BankTransactionRef);

            return MapToPayoutDto(payout);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PayoutDto> MarkUnknownPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminMarkUnknownPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Lý do trạng thái chưa rõ không được để trống.");

        var payout = await _unitOfWork.Payouts.GetByIdWithDetailsAsync(payoutId, cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException("Không tìm thấy lệnh rút tiền.");

        // Tuyệt đối không tự động hoàn tiền khi trạng thái là Unknown
        payout.MarkUnknown(request.Reason, adminId);
        _unitOfWork.Payouts.Update(payout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Admin {AdminId} đã đánh dấu lệnh rút tiền #{PayoutId} là Unknown (Chờ đối soát). Lý do: {Reason}",
            adminId, payout.Id, request.Reason);

        return MapToPayoutDto(payout);
    }

    private static ShopBankAccountDto MapToBankAccountDto(ShopBankAccount account)
    {
        return new ShopBankAccountDto
        {
            Id = account.Id,
            ShopId = account.ShopId,
            BankCode = account.BankCode,
            BankName = account.BankName,
            AccountNumberMasked = account.AccountNumberMasked,
            AccountHolderName = account.AccountHolderName,
            Status = account.Status,
            IsLocked = account.LockedUntilUtc.HasValue && account.LockedUntilUtc.Value > DateTime.UtcNow,
            LockedUntilUtc = account.LockedUntilUtc,
            CreatedAtUtc = account.CreatedAtUtc
        };
    }

    private static PayoutDto MapToPayoutDto(Payout payout)
    {
        return new PayoutDto
        {
            Id = payout.Id,
            ShopId = payout.ShopId,
            ShopName = payout.Shop?.Name,
            Amount = payout.Amount,
            Fee = payout.Fee,
            NetAmount = payout.NetAmount,
            Status = payout.Status,
            BankCode = payout.ShopBankAccount?.BankCode ?? string.Empty,
            BankName = payout.ShopBankAccount?.BankName ?? string.Empty,
            AccountNumberMasked = payout.ShopBankAccount?.AccountNumberMasked ?? string.Empty,
            AccountHolderName = payout.ShopBankAccount?.AccountHolderName ?? string.Empty,
            BankTransactionRef = payout.BankTransactionRef,
            FailureReason = payout.FailureReason,
            AdminNote = payout.AdminNote,
            CreatedAtUtc = payout.CreatedAtUtc,
            ProcessedAtUtc = payout.ProcessedAtUtc,
            CompletedAtUtc = payout.CompletedAtUtc,
            StatusHistories = payout.StatusHistories.Select(h => new PayoutHistoryDto
            {
                Id = h.Id,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ActorType = h.ActorType,
                Note = h.Note,
                CreatedAtUtc = h.CreatedAtUtc
            }).ToList()
        };
    }
}
