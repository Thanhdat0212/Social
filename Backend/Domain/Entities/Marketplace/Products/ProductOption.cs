using Domain.Common;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Trục tùy chọn của sản phẩm (Ví dụ: "Thời hạn", "Gói dung lượng", "Hệ điều hành")
/// </summary>
public class ProductOption : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; } = 0;

    // Navigation
    public Product? Product { get; private set; }
    public ICollection<ProductOptionValue> Values { get; private set; } = new List<ProductOptionValue>();

    protected ProductOption() { }

    public ProductOption(Guid productId, string name, int position = 0)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên tùy chọn không được để trống.", nameof(name));

        ProductId = productId;
        Name = name.Trim();
        Position = position;
    }

    public void Update(string name, int position)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên tùy chọn không được để trống.", nameof(name));
        Name = name.Trim();
        Position = position;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
