using Domain.Common;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Categories;

/// <summary>
/// Yêu cầu đề xuất mở thêm danh mục sản phẩm từ phía Seller
/// </summary>
public class CategoryRequest : BaseEntity
{
    public Guid ShopId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? SuggestedParentId { get; private set; }
    public string? ReasonNote { get; private set; }
    public CategoryRequestStatus Status { get; private set; } = CategoryRequestStatus.Pending;
    public string? AdminNote { get; private set; }
    public Guid? ResolvedCategoryId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }

    // Navigation properties
    public Shop? Shop { get; private set; }
    public Category? SuggestedParent { get; private set; }
    public Category? ResolvedCategory { get; private set; }

    protected CategoryRequest() { }

    public CategoryRequest(
        Guid shopId,
        string name,
        Guid? suggestedParentId = null,
        string? reasonNote = null)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên danh mục đề xuất không được để trống.", nameof(name));

        ShopId = shopId;
        Name = name.Trim();
        SuggestedParentId = suggestedParentId;
        ReasonNote = reasonNote?.Trim();
        Status = CategoryRequestStatus.Pending;
    }

    public void Approve(Guid adminId, Guid resolvedCategoryId, string? adminNote = null)
    {
        if (Status != CategoryRequestStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể duyệt yêu cầu đang ở trạng thái chờ duyệt (Pending).");

        Status = CategoryRequestStatus.Approved;
        ResolvedCategoryId = resolvedCategoryId;
        ReviewedByAdminId = adminId;
        AdminNote = adminNote?.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string adminNote)
    {
        if (Status != CategoryRequestStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể từ chối yêu cầu đang ở trạng thái chờ duyệt (Pending).");
        if (string.IsNullOrWhiteSpace(adminNote))
            throw new ArgumentException("Từ chối yêu cầu bắt buộc phải có lý do/ghi chú từ Admin.", nameof(adminNote));

        Status = CategoryRequestStatus.Rejected;
        ReviewedByAdminId = adminId;
        AdminNote = adminNote.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
