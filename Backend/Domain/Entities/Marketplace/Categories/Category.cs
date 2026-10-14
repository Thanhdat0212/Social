using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Categories;

/// <summary>
/// Thực thể Danh mục hàng hóa (Cây danh mục tối đa 3 cấp)
/// </summary>
public class Category : BaseEntity
{
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public int Depth { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ProductType ProductType { get; private set; } = ProductType.Digital;
    public CategoryModerationPolicy ModerationPolicy { get; private set; } = CategoryModerationPolicy.DirectListing;
    public int? CommissionRateBp { get; private set; }

    // Navigation properties
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<CategoryAttribute> CategoryAttributes { get; private set; } = new List<CategoryAttribute>();

    protected Category() { }

    public Category(
        string name,
        string slug,
        Guid? parentId = null,
        string? parentPath = null,
        int parentDepth = -1,
        int displayOrder = 0,
        ProductType productType = ProductType.Digital,
        CategoryModerationPolicy moderationPolicy = CategoryModerationPolicy.DirectListing,
        int? commissionRateBp = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên danh mục không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug danh mục không được để trống.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ParentId = parentId;
        DisplayOrder = displayOrder;
        ProductType = productType;
        ModerationPolicy = moderationPolicy;
        CommissionRateBp = commissionRateBp;
        IsActive = true;

        if (parentId.HasValue)
        {
            Depth = parentDepth + 1;
            Path = string.IsNullOrWhiteSpace(parentPath) ? $"/{Slug}" : $"{parentPath.TrimEnd('/')}/{Slug}";
        }
        else
        {
            Depth = 0;
            Path = $"/{Slug}";
        }
    }

    public void Update(
        string name,
        string slug,
        int displayOrder,
        ProductType productType,
        CategoryModerationPolicy moderationPolicy,
        int? commissionRateBp)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên danh mục không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug danh mục không được để trống.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
        ProductType = productType;
        ModerationPolicy = moderationPolicy;
        CommissionRateBp = commissionRateBp;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetParent(Category? parent)
    {
        if (parent == null)
        {
            ParentId = null;
            Depth = 0;
            Path = $"/{Slug}";
        }
        else
        {
            if (parent.Id == Id)
                throw new InvalidOperationException("Không thể gán danh mục cha là chính nó.");
            if (parent.Depth >= 2)
                throw new InvalidOperationException("Cây danh mục chỉ hỗ trợ tối đa 3 cấp (Depth: 0, 1, 2).");

            ParentId = parent.Id;
            Depth = parent.Depth + 1;
            Path = $"{parent.Path.TrimEnd('/')}/{Slug}";
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePathAndDepth(string? parentPath, int parentDepth)
    {
        if (ParentId.HasValue)
        {
            Depth = parentDepth + 1;
            Path = string.IsNullOrWhiteSpace(parentPath) ? $"/{Slug}" : $"{parentPath.TrimEnd('/')}/{Slug}";
        }
        else
        {
            Depth = 0;
            Path = $"/{Slug}";
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
