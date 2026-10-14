using Domain.Common;
using Domain.Entities.Marketplace.Categories;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Thực thể Sản phẩm chính (Product) trên Marketplace
/// </summary>
public class Product : BaseEntity
{
    public Guid ShopId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    
    /// <summary>
    /// Bộ đệm giá nhỏ nhất của các biến thể (VND) để hiển thị nhanh trên danh sách
    /// </summary>
    public long MinPriceCache { get; private set; } = 0;

    /// <summary>
    /// Bộ đệm giá lớn nhất của các biến thể (VND)
    /// </summary>
    public long MaxPriceCache { get; private set; } = 0;

    /// <summary>
    /// Điểm đánh giá trung bình (1.0 -> 5.0)
    /// </summary>
    public double RatingAverage { get; private set; } = 0.0;

    /// <summary>
    /// Tổng số lượt đánh giá hợp lệ
    /// </summary>
    public int ReviewCount { get; private set; } = 0;

    public DateTime? DeletedAtUtc { get; private set; }

    // Navigation properties
    public Shop? Shop { get; private set; }
    public Category? Category { get; private set; }
    public ICollection<ProductOption> Options { get; private set; } = new List<ProductOption>();
    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public ICollection<ProductAttributeValue> AttributeValues { get; private set; } = new List<ProductAttributeValue>();
    public ICollection<ProductTakedown> Takedowns { get; private set; } = new List<ProductTakedown>();
    public ICollection<PostProduct> PostProducts { get; private set; } = new List<PostProduct>();

    // EF Core constructor
    protected Product() { }

    public Product(Guid shopId, string name, string slug, Guid? categoryId = null, string? description = null)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên sản phẩm không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug sản phẩm không được để trống.", nameof(slug));

        ShopId = shopId;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        CategoryId = categoryId;
        Description = description?.Trim();
        Status = ProductStatus.Draft;
        MinPriceCache = 0;
        MaxPriceCache = 0;
    }

    public void UpdateDetails(string name, string slug, string? description, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên sản phẩm không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug sản phẩm không được để trống.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        CategoryId = categoryId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish(bool requiresReview = false)
    {
        if (Status == ProductStatus.Archived)
        {
            throw new InvalidOperationException("Không thể đăng bán sản phẩm đã lưu trữ (Archived).");
        }

        Status = requiresReview ? ProductStatus.PendingReview : ProductStatus.Listed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Hide()
    {
        if (Status != ProductStatus.Listed)
        {
            throw new InvalidOperationException("Chỉ có thể ẩn sản phẩm đang ở trạng thái Listed.");
        }
        Status = ProductStatus.Hidden;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unhide()
    {
        if (Status != ProductStatus.Hidden)
        {
            throw new InvalidOperationException("Chỉ có thể bỏ ẩn sản phẩm đang ở trạng thái Hidden.");
        }
        Status = ProductStatus.Listed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePriceCache(long minPrice, long maxPrice)
    {
        MinPriceCache = minPrice;
        MaxPriceCache = maxPrice;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool HasActiveTakedown() => Takedowns.Any(t => t.LiftedAtUtc == null);

    public void UpdateRating(double ratingAverage, int reviewCount)
    {
        RatingAverage = Math.Round(Math.Clamp(ratingAverage, 0.0, 5.0), 1);
        ReviewCount = Math.Max(0, reviewCount);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
