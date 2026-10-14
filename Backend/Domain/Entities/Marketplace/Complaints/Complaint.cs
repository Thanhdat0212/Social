using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Complaints;

/// <summary>
/// Thực thể Khiếu nại đơn hàng (Complaint) - Bảo vệ người mua khi mã số/file bị lỗi
/// </summary>
public class Complaint : BaseEntity
{
    public Guid ShopOrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid BuyerUserId { get; private set; }
    public Guid ShopId { get; private set; }

    public ComplaintReason Reason { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int FaultyQuantity { get; private set; } = 1;

    public ComplaintStatus Status { get; private set; } = ComplaintStatus.Submitted;
    public DateTime ShopDeadlineAtUtc { get; private set; }

    // Phản hồi của Chủ Shop
    public string? ShopResponseNote { get; private set; }
    public DateTime? ShopRespondedAtUtc { get; private set; }

    // Phán quyết của Quản trị viên (Admin)
    public Guid? ResolvedByAdminId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? AdminResolutionNote { get; private set; }
    public string? ResolutionType { get; private set; } // "Refund" | "Replacement" | "Rejected"

    // Navigation
    public ShopOrder? ShopOrder { get; private set; }
    public OrderItem? OrderItem { get; private set; }
    public User? BuyerUser { get; private set; }
    public Shop? Shop { get; private set; }
    public User? ResolvedByAdmin { get; private set; }
    public ICollection<ComplaintEvidence> Evidences { get; private set; } = new List<ComplaintEvidence>();

    protected Complaint() { }

    public Complaint(
        Guid shopOrderId,
        Guid orderItemId,
        Guid buyerUserId,
        Guid shopId,
        ComplaintReason reason,
        string description,
        int faultyQuantity = 1,
        int shopDeadlineHours = 24)
    {
        if (shopOrderId == Guid.Empty) throw new ArgumentException("ShopOrderId không hợp lệ.", nameof(shopOrderId));
        if (orderItemId == Guid.Empty) throw new ArgumentException("OrderItemId không hợp lệ.", nameof(orderItemId));
        if (buyerUserId == Guid.Empty) throw new ArgumentException("BuyerUserId không hợp lệ.", nameof(buyerUserId));
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Mô tả lỗi không được để trống.", nameof(description));
        if (faultyQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(faultyQuantity), "Số lượng lỗi phải lớn hơn 0.");

        ShopOrderId = shopOrderId;
        OrderItemId = orderItemId;
        BuyerUserId = buyerUserId;
        ShopId = shopId;
        Reason = reason;
        Description = description.Trim();
        FaultyQuantity = faultyQuantity;
        Status = ComplaintStatus.Submitted;
        ShopDeadlineAtUtc = DateTime.UtcNow.AddHours(shopDeadlineHours);
    }

    public void AddEvidence(ComplaintEvidence evidence)
    {
        Evidences.Add(evidence);
    }

    /// <summary>
    /// Shop chấp nhận khiếu nại (đồng ý hoàn tiền hoặc đổi mã mới)
    /// </summary>
    public void ShopAccept(string? note, string resolutionType)
    {
        if (Status != ComplaintStatus.Submitted && Status != ComplaintStatus.EscalatedToAdmin)
        {
            throw new InvalidOperationException($"Không thể chấp nhận khiếu nại đang ở trạng thái {Status}.");
        }

        Status = ComplaintStatus.ShopAccepted;
        ShopResponseNote = note?.Trim();
        ShopRespondedAtUtc = DateTime.UtcNow;
        ResolutionType = resolutionType.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Shop từ chối khiếu nại -> Tự động chuyển lên Admin phân xử
    /// </summary>
    public void ShopReject(string note)
    {
        if (Status != ComplaintStatus.Submitted)
        {
            throw new InvalidOperationException($"Không thể từ chối khiếu nại đang ở trạng thái {Status}.");
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Shop từ chối bắt buộc phải cung cấp lý do giải trình.", nameof(note));
        }

        Status = ComplaintStatus.ShopRejected;
        ShopResponseNote = note.Trim();
        ShopRespondedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Chuyển Admin khi quá hạn 24h shop không trả lời
    /// </summary>
    public void EscalateToAdmin()
    {
        if (Status == ComplaintStatus.Submitted || Status == ComplaintStatus.ShopRejected)
        {
            Status = ComplaintStatus.EscalatedToAdmin;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Admin đưa ra phán quyết cuối cùng
    /// </summary>
    public void AdminResolve(Guid adminId, ComplaintStatus finalStatus, string resolutionType, string note)
    {
        if (finalStatus != ComplaintStatus.ResolvedRefund &&
            finalStatus != ComplaintStatus.ResolvedReplacement &&
            finalStatus != ComplaintStatus.Rejected)
        {
            throw new ArgumentException("Trạng thái phán quyết không hợp lệ.", nameof(finalStatus));
        }

        ResolvedByAdminId = adminId;
        Status = finalStatus;
        ResolutionType = resolutionType;
        AdminResolutionNote = note.Trim();
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Người mua tự hủy khiếu nại
    /// </summary>
    public void BuyerCancel()
    {
        if (Status != ComplaintStatus.Submitted)
        {
            throw new InvalidOperationException("Chỉ có thể hủy khiếu nại khi chưa có phản hồi hoặc xử lý.");
        }

        Status = ComplaintStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
