using Domain.Common;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Hình ảnh sản phẩm (ProductImage)
/// </summary>
public class ProductImage : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int Position { get; private set; } = 0;
    public bool IsCover { get; private set; } = false;

    // Navigation
    public Product? Product { get; private set; }

    protected ProductImage() { }

    public ProductImage(Guid productId, string url, int position = 0, bool isCover = false)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL ảnh không được để trống.", nameof(url));

        ProductId = productId;
        Url = url.Trim();
        Position = position;
        IsCover = isCover;
    }

    public void UpdatePosition(int position, bool isCover)
    {
        Position = position;
        IsCover = isCover;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetCover(bool isCover)
    {
        IsCover = isCover;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
