using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Shops;

/// <summary>
/// Thực thể Gian hàng (Shop) của người bán trên sàn thương mại số
/// </summary>
public class Shop : BaseEntity
{
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Pending;
    public OperatingStatus OperatingStatus { get; private set; } = OperatingStatus.Open;
    public DateTime? DeletedAtUtc { get; private set; }

    // Navigation properties
    public User? OwnerUser { get; private set; }
    public ICollection<ShopReview> Reviews { get; private set; } = new List<ShopReview>();
    public ICollection<ShopSanction> Sanctions { get; private set; } = new List<ShopSanction>();

    // EF Core parameterless constructor
    protected Shop() { }

    public Shop(
        Guid ownerUserId,
        string name,
        string slug,
        string contactEmail,
        string? description = null,
        string? logoUrl = null)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("OwnerUserId không hợp lệ.", nameof(ownerUserId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên gian hàng không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug gian hàng không được để trống.", nameof(slug));
        if (string.IsNullOrWhiteSpace(contactEmail)) throw new ArgumentException("Email liên hệ không được để trống.", nameof(contactEmail));

        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ContactEmail = contactEmail.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        ApprovalStatus = ApprovalStatus.Pending;
        OperatingStatus = OperatingStatus.Open;
    }

    public void Approve()
    {
        if (ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể duyệt gian hàng đang ở trạng thái Pending. Hiện tại: '{ApprovalStatus}'.");
        }
        ApprovalStatus = ApprovalStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể từ chối gian hàng đang ở trạng thái Pending. Hiện tại: '{ApprovalStatus}'.");
        }
        ApprovalStatus = ApprovalStatus.Rejected;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Resubmit(string name, string? description, string? logoUrl, string contactEmail)
    {
        if (ApprovalStatus != ApprovalStatus.Rejected)
        {
            throw new InvalidOperationException("Chỉ có thể nộp lại hồ sơ khi gian hàng đang bị Từ chối (Rejected).");
        }

        UpdateDetails(name, description, logoUrl, contactEmail);
        ApprovalStatus = ApprovalStatus.Pending;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description, string? logoUrl, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên gian hàng không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(contactEmail)) throw new ArgumentException("Email liên hệ không được để trống.", nameof(contactEmail));

        Name = name.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        ContactEmail = contactEmail.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Open()
    {
        OperatingStatus = OperatingStatus.Open;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        OperatingStatus = OperatingStatus.Closed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
