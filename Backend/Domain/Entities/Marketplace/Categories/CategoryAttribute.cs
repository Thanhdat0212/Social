using Domain.Enums;

namespace Domain.Entities.Marketplace.Categories;

/// <summary>
/// Liên kết gán thuộc tính vào danh mục và cấu hình quy tắc (Bắt buộc, Bộ lọc, Trục biến thể)
/// </summary>
public class CategoryAttribute
{
    public Guid CategoryId { get; private set; }
    public Guid AttributeId { get; private set; }
    public AttributeRequirement Requirement { get; private set; } = AttributeRequirement.Optional;
    public bool IsFilterable { get; private set; }
    public bool IsVariantAxis { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public Category? Category { get; private set; }
    public AttributeDefinition? AttributeDefinition { get; private set; }

    protected CategoryAttribute() { }

    public CategoryAttribute(
        Guid categoryId,
        Guid attributeId,
        AttributeRequirement requirement = AttributeRequirement.Optional,
        bool isFilterable = false,
        bool isVariantAxis = false)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("CategoryId không hợp lệ.", nameof(categoryId));
        if (attributeId == Guid.Empty) throw new ArgumentException("AttributeId không hợp lệ.", nameof(attributeId));

        CategoryId = categoryId;
        AttributeId = attributeId;
        Requirement = requirement;
        IsFilterable = isFilterable;
        IsVariantAxis = isVariantAxis;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        AttributeRequirement requirement,
        bool isFilterable,
        bool isVariantAxis)
    {
        Requirement = requirement;
        IsFilterable = isFilterable;
        IsVariantAxis = isVariantAxis;
    }
}
