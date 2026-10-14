using Domain.Common;

namespace Domain.Entities.Marketplace.Categories;

/// <summary>
/// Tùy chọn giá trị cho các thuộc tính kiểu Enum (Ví dụ: Windows 10, Windows 11, Netflix 1 Tháng...)
/// </summary>
public class AttributeOption : BaseEntity
{
    public Guid AttributeId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation property
    public AttributeDefinition? AttributeDefinition { get; private set; }

    protected AttributeOption() { }

    public AttributeOption(
        Guid attributeId,
        string value,
        string slug,
        int displayOrder = 0)
    {
        if (attributeId == Guid.Empty) throw new ArgumentException("AttributeId không hợp lệ.", nameof(attributeId));
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Giá trị thuộc tính không được để trống.", nameof(value));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug không được để trống.", nameof(slug));

        AttributeId = attributeId;
        Value = value.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void Update(string value, string slug, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Giá trị thuộc tính không được để trống.", nameof(value));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug không được để trống.", nameof(slug));

        Value = value.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
