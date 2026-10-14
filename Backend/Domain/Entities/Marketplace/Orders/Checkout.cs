using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Orders;

/// <summary>
/// Phiên thanh toán (Checkout) - Bản chụp những sản phẩm người mua quyết định mua
/// </summary>
public class Checkout : BaseEntity
{
    public string CheckoutNo { get; private set; } = string.Empty;
    public Guid BuyerUserId { get; private set; }
    public CheckoutStatus Status { get; private set; } = CheckoutStatus.Open;

    public long ItemsTotal { get; private set; } = 0;
    public long DiscountTotal { get; private set; } = 0;
    public long GrandTotal { get; private set; } = 0;

    public string? IdempotencyKey { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    // Navigation
    public User? BuyerUser { get; private set; }
    public ICollection<CheckoutItem> Items { get; private set; } = new List<CheckoutItem>();

    protected Checkout() { }

    public Checkout(
        string checkoutNo,
        Guid buyerUserId,
        long itemsTotal,
        long discountTotal,
        long grandTotal,
        DateTime expiresAtUtc,
        string? idempotencyKey = null)
    {
        if (string.IsNullOrWhiteSpace(checkoutNo)) throw new ArgumentException("CheckoutNo không hợp lệ.", nameof(checkoutNo));
        if (buyerUserId == Guid.Empty) throw new ArgumentException("BuyerUserId không hợp lệ.", nameof(buyerUserId));
        if (grandTotal <= 0) throw new ArgumentException("Tổng số tiền phải lớn hơn 0.", nameof(grandTotal));

        CheckoutNo = checkoutNo.Trim().ToUpperInvariant();
        BuyerUserId = buyerUserId;
        ItemsTotal = itemsTotal;
        DiscountTotal = discountTotal;
        GrandTotal = grandTotal;
        ExpiresAtUtc = expiresAtUtc;
        IdempotencyKey = idempotencyKey?.Trim();
        Status = CheckoutStatus.Open;
    }

    public void MarkPaid()
    {
        if (Status != CheckoutStatus.Open)
        {
            throw new InvalidOperationException($"Không thể đánh dấu thanh toán cho Checkout đang ở trạng thái '{Status}'.");
        }

        Status = CheckoutStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != CheckoutStatus.Open) return;

        Status = CheckoutStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != CheckoutStatus.Open) return;

        Status = CheckoutStatus.Failed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Dòng sản phẩm trong phiên thanh toán (Bản chụp thông tin tại thời điểm chốt mua)
/// </summary>
public class CheckoutItem : BaseEntity
{
    public Guid CheckoutId { get; private set; }
    public Guid ShopId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? CategoryId { get; private set; }

    public int Quantity { get; private set; }
    public long UnitPrice { get; private set; }
    public long LineTotal { get; private set; }

    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public string VariantLabelSnapshot { get; private set; } = string.Empty;
    public string SkuSnapshot { get; private set; } = string.Empty;

    /// <summary>
    /// Tỷ lệ hoa hồng tính theo Basis Points (1000 = 10%)
    /// </summary>
    public int CommissionRateBp { get; private set; } = 0;

    /// <summary>
    /// Số tiền hoa hồng sàn thu (VND)
    /// </summary>
    public long CommissionAmount { get; private set; } = 0;

    /// <summary>
    /// Số tiền thực nhận của Shop sau khi trừ hoa hồng (VND)
    /// </summary>
    public long ShopNet { get; private set; } = 0;

    public DeliveryType DeliveryType { get; private set; } = DeliveryType.CodePool;

    // Navigation
    public Checkout? Checkout { get; private set; }
    public Shop? Shop { get; private set; }
    public ProductVariant? Variant { get; private set; }
    public Product? Product { get; private set; }

    protected CheckoutItem() { }

    public CheckoutItem(
        Guid checkoutId,
        Guid shopId,
        Guid variantId,
        Guid productId,
        Guid? categoryId,
        int quantity,
        long unitPrice,
        long lineTotal,
        string productNameSnapshot,
        string variantLabelSnapshot,
        string skuSnapshot,
        int commissionRateBp,
        long commissionAmount,
        long shopNet,
        DeliveryType deliveryType = DeliveryType.CodePool)
    {
        CheckoutId = checkoutId;
        ShopId = shopId;
        VariantId = variantId;
        ProductId = productId;
        CategoryId = categoryId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
        ProductNameSnapshot = productNameSnapshot.Trim();
        VariantLabelSnapshot = variantLabelSnapshot.Trim();
        SkuSnapshot = skuSnapshot.Trim().ToUpperInvariant();
        CommissionRateBp = commissionRateBp;
        CommissionAmount = commissionAmount;
        ShopNet = shopNet;
        DeliveryType = deliveryType;
    }
}
