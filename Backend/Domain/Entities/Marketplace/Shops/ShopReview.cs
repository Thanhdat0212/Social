using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Shops;

/// <summary>
/// Lịch sử các lần nộp hồ sơ và quyết định xét duyệt gian hàng bởi Admin
/// </summary>
public class ShopReview : BaseEntity
{
    public Guid ShopId { get; private set; }
    public int SubmissionNo { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public ShopReviewDecision Decision { get; private set; } = ShopReviewDecision.Pending;
    public string? ReasonCode { get; private set; }
    public string? ReasonNote { get; private set; }
    public bool CanResubmit { get; private set; } = true;
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    // Navigation property
    public Shop? Shop { get; private set; }

    protected ShopReview() { }

    public ShopReview(Guid shopId, int submissionNo, string snapshotJson)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (submissionNo < 1) throw new ArgumentOutOfRangeException(nameof(submissionNo), "SubmissionNo phải lớn hơn hoặc bằng 1.");
        if (string.IsNullOrWhiteSpace(snapshotJson)) throw new ArgumentException("SnapshotJson không được để trống.", nameof(snapshotJson));

        ShopId = shopId;
        SubmissionNo = submissionNo;
        SnapshotJson = snapshotJson;
        Decision = ShopReviewDecision.Pending;
        CanResubmit = true;
    }

    public void Approve(Guid adminId)
    {
        Decision = ShopReviewDecision.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string reasonCode, string reasonNote, bool canResubmit = true)
    {
        if (string.IsNullOrWhiteSpace(reasonNote))
        {
            throw new ArgumentException("Lý do từ chối không được để trống.", nameof(reasonNote));
        }

        Decision = ShopReviewDecision.Rejected;
        ReviewedByAdminId = adminId;
        ReasonCode = reasonCode.Trim();
        ReasonNote = reasonNote.Trim();
        CanResubmit = canResubmit;
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
