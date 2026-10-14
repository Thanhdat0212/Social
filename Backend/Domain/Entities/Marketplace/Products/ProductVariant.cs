using Domain.Common;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Biến thể sản phẩm (Variant) - Đơn vị thực tế được giao dịch, định giá và kiểm soát tồn kho
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid ShopId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string SkuNormalized { get; private set; } = string.Empty;
    public string CombinationKey { get; private set; } = "default";
    
    /// <summary>
    /// Giá bán thực tế (VND, số nguyên lớn)
    /// </summary>
    public long Price { get; private set; }

    /// <summary>
    /// Giá niêm yết/so sánh gạch ngang (nếu có khuyến mãi)
    /// </summary>
    public long? CompareAtPrice { get; private set; }

    public DeliveryType DeliveryType { get; private set; } = DeliveryType.CodePool;
    public bool TrackInventory { get; private set; } = true;
    public DateTime? DeletedAtUtc { get; private set; }

    // Navigation
    public Product? Product { get; private set; }
    public Shop? Shop { get; private set; }
    public ICollection<VariantOptionValue> OptionValues { get; private set; } = new List<VariantOptionValue>();

    protected ProductVariant() { }

    public ProductVariant(
        Guid productId,
        Guid shopId,
        string sku,
        string combinationKey,
        long price,
        long? compareAtPrice = null,
        DeliveryType deliveryType = DeliveryType.CodePool,
        bool trackInventory = true)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU không được để trống.", nameof(sku));
        if (price <= 0) throw new ArgumentException("Giá sản phẩm phải lớn hơn 0.", nameof(price));
        if (compareAtPrice.HasValue && compareAtPrice.Value < price)
            throw new ArgumentException("Giá gốc so sánh không được nhỏ hơn giá bán.", nameof(compareAtPrice));

        ProductId = productId;
        ShopId = shopId;
        Sku = sku.Trim();
        SkuNormalized = Sku.ToUpperInvariant();
        CombinationKey = string.IsNullOrWhiteSpace(combinationKey) ? "default" : combinationKey.Trim();
        Price = price;
        CompareAtPrice = compareAtPrice;
        DeliveryType = deliveryType;
        TrackInventory = trackInventory;
    }

    public void Update(long price, long? compareAtPrice, DeliveryType deliveryType, bool trackInventory)
    {
        if (price <= 0) throw new ArgumentException("Giá sản phẩm phải lớn hơn 0.", nameof(price));
        if (compareAtPrice.HasValue && compareAtPrice.Value < price)
            throw new ArgumentException("Giá gốc so sánh không được nhỏ hơn giá bán.", nameof(compareAtPrice));

        Price = price;
        CompareAtPrice = compareAtPrice;
        DeliveryType = deliveryType;
        TrackInventory = trackInventory;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU không được để trống.", nameof(sku));
        Sku = sku.Trim();
        SkuNormalized = Sku.ToUpperInvariant();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
