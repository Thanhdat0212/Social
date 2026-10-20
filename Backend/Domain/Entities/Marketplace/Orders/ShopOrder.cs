using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Categories;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Orders;

/// <summary>
/// Đơn hàng của một gian hàng (ShopOrder) - Được sinh ra SAU KHI THANH TOÁN THÀNH CÔNG
/// </summary>
public class ShopOrder : BaseEntity
{
    public string OrderNo { get; private set; } = string.Empty;
    public Guid CheckoutId { get; private set; }
    public Guid ShopId { get; private set; }
    public Guid BuyerUserId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Delivered;

    public long Total { get; private set; } = 0;
    public long ShopGross { get; private set; } = 0;
    public long CommissionTotal { get; private set; } = 0;
    public long ShopNet { get; private set; } = 0;

    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ComplaintWindowEndsAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public OrderSource Source { get; private set; } = OrderSource.Marketplace;
    public string? ExternalCustomerInfo { get; private set; }
    public string? Note { get; private set; }

    // Navigation
    public Checkout? Checkout { get; private set; }
    public Shop? Shop { get; private set; }
    public User? BuyerUser { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> Histories { get; private set; } = new List<OrderStatusHistory>();
    public ICollection<Fulfillment> Fulfillments { get; private set; } = new List<Fulfillment>();

    protected ShopOrder() { }

    public ShopOrder(
        string orderNo,
        Guid checkoutId,
        Guid shopId,
        Guid buyerUserId,
        long total,
        long shopGross,
        long commissionTotal,
        long shopNet,
        DateTime deliveredAtUtc,
        DateTime complaintWindowEndsAtUtc)
    {
        if (string.IsNullOrWhiteSpace(orderNo)) throw new ArgumentException("OrderNo không hợp lệ.", nameof(orderNo));
        if (checkoutId == Guid.Empty) throw new ArgumentException("CheckoutId không hợp lệ.", nameof(checkoutId));
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (buyerUserId == Guid.Empty) throw new ArgumentException("BuyerUserId không hợp lệ.", nameof(buyerUserId));

        OrderNo = orderNo.Trim().ToUpperInvariant();
        CheckoutId = checkoutId;
        ShopId = shopId;
        BuyerUserId = buyerUserId;
        Total = total;
        ShopGross = shopGross;
        CommissionTotal = commissionTotal;
        ShopNet = shopNet;
        DeliveredAtUtc = deliveredAtUtc;
        ComplaintWindowEndsAtUtc = complaintWindowEndsAtUtc;
        Status = OrderStatus.Delivered;
    }

    public void MarkCompleted()
    {
        if (Status != OrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Chỉ có thể chuyển hoàn tất cho đơn hàng đang ở trạng thái Delivered.");
        }

        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSourceInfo(OrderSource source, string? customerInfo = null, string? note = null)
    {
        Source = source;
        ExternalCustomerInfo = customerInfo?.Trim();
        Note = note?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Chi tiết từng món hàng trong đơn hàng
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid ShopOrderId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? CategoryId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;
    public string VariantLabel { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;

    public long UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public long LineTotal { get; private set; }
    public long ShopGross { get; private set; }

    public int CommissionRateBp { get; private set; } = 0;
    public long CommissionAmount { get; private set; } = 0;
    public long ShopNet { get; private set; } = 0;

    public DeliveryType DeliveryType { get; private set; } = DeliveryType.CodePool;
    public int RefundedQty { get; private set; } = 0;
    public int DownloadCount { get; private set; } = 0;

    // Navigation
    public ShopOrder? ShopOrder { get; private set; }
    public ProductVariant? Variant { get; private set; }
    public Product? Product { get; private set; }
    public Category? Category { get; private set; }

    protected OrderItem() { }

    public OrderItem(
        Guid shopOrderId,
        Guid variantId,
        Guid productId,
        Guid? categoryId,
        string productName,
        string variantLabel,
        string sku,
        long unitPrice,
        int quantity,
        long lineTotal,
        long shopGross,
        int commissionRateBp,
        long commissionAmount,
        long shopNet,
        DeliveryType deliveryType)
    {
        ShopOrderId = shopOrderId;
        VariantId = variantId;
        ProductId = productId;
        CategoryId = categoryId;
        ProductName = productName.Trim();
        VariantLabel = variantLabel.Trim();
        Sku = sku.Trim().ToUpperInvariant();
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = lineTotal;
        ShopGross = shopGross;
        CommissionRateBp = commissionRateBp;
        CommissionAmount = commissionAmount;
        ShopNet = shopNet;
        DeliveryType = deliveryType;
        RefundedQty = 0;
    }

    public void AddRefundedQty(int qty)
    {
        RefundedQty += qty;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncrementDownloadCount()
    {
        DownloadCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
