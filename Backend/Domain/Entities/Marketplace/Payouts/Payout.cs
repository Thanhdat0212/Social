using Domain.Common;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Payouts;

/// <summary>
/// Lệnh rút tiền của Gian hàng (Payout)
/// </summary>
public class Payout : BaseEntity
{
    public Guid ShopId { get; private set; }
    public Guid ShopBankAccountId { get; private set; }
    public Guid? PayoutBatchId { get; private set; } // Hỗ trợ gom đợt thanh toán (batch)

    public long Amount { get; private set; }         // Số tiền yêu cầu rút (VND)
    public long Fee { get; private set; }            // Phí xử lý rút tiền (nếu có)
    public long NetAmount { get; private set; }       // Số tiền thực nhận = Amount - Fee

    public PayoutStatus Status { get; private set; } = PayoutStatus.Requested;
    public string RequestIdempotencyKey { get; private set; } = string.Empty; // Chống duplicate submit
    public string? BankTransactionRef { get; private set; }                   // Mã tham chiếu ngân hàng
    public string? FailureReason { get; private set; }
    public string? AdminNote { get; private set; }
    public Guid? ProcessedByAdminId { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    // Navigation
    public Shop? Shop { get; private set; }
    public ShopBankAccount? ShopBankAccount { get; private set; }

    private readonly List<PayoutStatusHistory> _statusHistories = new();
    public IReadOnlyCollection<PayoutStatusHistory> StatusHistories => _statusHistories.AsReadOnly();

    protected Payout() { }

    public Payout(
        Guid shopId,
        Guid shopBankAccountId,
        long amount,
        long fee,
        string requestIdempotencyKey)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (shopBankAccountId == Guid.Empty) throw new ArgumentException("ShopBankAccountId không hợp lệ.", nameof(shopBankAccountId));
        if (amount < 100_000) throw new ArgumentException("Số tiền rút tối thiểu là 100.000 VNĐ.", nameof(amount));
        if (fee < 0) throw new ArgumentException("Phí không thể âm.", nameof(fee));
        if (fee >= amount) throw new ArgumentException("Phí không thể lớn hơn hoặc bằng số tiền rút.", nameof(fee));
        if (string.IsNullOrWhiteSpace(requestIdempotencyKey)) throw new ArgumentException("Idempotency key không được để trống.", nameof(requestIdempotencyKey));

        ShopId = shopId;
        ShopBankAccountId = shopBankAccountId;
        Amount = amount;
        Fee = fee;
        NetAmount = amount - fee;
        RequestIdempotencyKey = requestIdempotencyKey.Trim();
        Status = PayoutStatus.Requested;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            PayoutStatus.Requested,
            PayoutStatus.Requested,
            null,
            PayoutActorType.ShopOwner,
            "Tạo yêu cầu rút tiền mới"));
    }

    public void Approve(Guid adminId, string? note = null)
    {
        if (Status != PayoutStatus.Requested)
            throw new InvalidOperationException($"Chỉ có thể phê duyệt lệnh rút đang ở trạng thái Requested (Hiện tại: {Status}).");

        var prev = Status;
        Status = PayoutStatus.Approved;
        ProcessedByAdminId = adminId;
        ProcessedAtUtc = DateTime.UtcNow;
        AdminNote = note;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            adminId,
            PayoutActorType.Admin,
            note ?? "Admin phê duyệt lệnh rút tiền"));
    }

    public void MarkProcessing(Guid? actorId = null, string? note = null)
    {
        if (Status != PayoutStatus.Approved && Status != PayoutStatus.Requested)
            throw new InvalidOperationException($"Chỉ có thể chuyển sang Processing từ Requested hoặc Approved (Hiện tại: {Status}).");

        var prev = Status;
        Status = PayoutStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            actorId,
            actorId.HasValue ? PayoutActorType.Admin : PayoutActorType.System,
            note ?? "Đang chuyển khoản tới ngân hàng"));
    }

    public void MarkSucceeded(string bankTransactionRef, Guid? actorId = null, string? note = null)
    {
        if (Status != PayoutStatus.Processing && Status != PayoutStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể hoàn thành lệnh rút từ Approved hoặc Processing (Hiện tại: {Status}).");

        var prev = Status;
        Status = PayoutStatus.Succeeded;
        BankTransactionRef = bankTransactionRef;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            actorId,
            actorId.HasValue ? PayoutActorType.Admin : PayoutActorType.System,
            note ?? $"Chuyển khoản thành công (Mã GD: {bankTransactionRef})"));
    }

    public void MarkFailed(string reason, Guid? actorId = null)
    {
        if (Status == PayoutStatus.Succeeded || Status == PayoutStatus.Cancelled || Status == PayoutStatus.Rejected)
            throw new InvalidOperationException($"Lệnh rút đã ở trạng thái kết thúc ({Status}), không thể đánh dấu Thất bại.");

        var prev = Status;
        Status = PayoutStatus.Failed;
        FailureReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            actorId,
            actorId.HasValue ? PayoutActorType.Admin : PayoutActorType.System,
            $"Chuyển khoản thất bại: {reason}"));
    }

    public void MarkUnknown(string reason, Guid? actorId = null)
    {
        var prev = Status;
        Status = PayoutStatus.Unknown;
        FailureReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            actorId,
            actorId.HasValue ? PayoutActorType.Admin : PayoutActorType.System,
            $"Giao dịch ngân hàng chưa rõ kết quả: {reason} (Chờ đối soát)"));
    }

    public void Reject(Guid adminId, string reason)
    {
        if (Status != PayoutStatus.Requested && Status != PayoutStatus.Approved)
            throw new InvalidOperationException($"Chỉ có thể từ chối lệnh rút ở trạng thái Requested hoặc Approved (Hiện tại: {Status}).");

        var prev = Status;
        Status = PayoutStatus.Rejected;
        ProcessedByAdminId = adminId;
        ProcessedAtUtc = DateTime.UtcNow;
        FailureReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            adminId,
            PayoutActorType.Admin,
            $"Admin từ chối lệnh rút: {reason}"));
    }

    public void CancelByShop(Guid shopOwnerId, string reason)
    {
        if (Status != PayoutStatus.Requested)
            throw new InvalidOperationException("Chỉ có thể tự hủy lệnh rút khi lệnh còn đang chờ duyệt (Requested).");

        var prev = Status;
        Status = PayoutStatus.Cancelled;
        FailureReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistories.Add(new PayoutStatusHistory(
            Id,
            prev,
            Status,
            shopOwnerId,
            PayoutActorType.ShopOwner,
            $"Shop tự hủy lệnh rút: {reason}"));
    }
}
