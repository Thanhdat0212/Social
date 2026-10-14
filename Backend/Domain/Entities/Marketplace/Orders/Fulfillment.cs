using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Orders;

/// <summary>
/// Thực thể Xuất giao hàng số (Fulfillment)
/// </summary>
public class Fulfillment : BaseEntity
{
    public Guid ShopOrderId { get; private set; }
    public FulfillmentStatus Status { get; private set; } = FulfillmentStatus.Fulfilled;
    public DateTime DeliveredAtUtc { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ShopOrder? ShopOrder { get; private set; }
    public ICollection<FulfillmentItem> Items { get; private set; } = new List<FulfillmentItem>();

    protected Fulfillment() { }

    public Fulfillment(Guid shopOrderId, FulfillmentStatus status = FulfillmentStatus.Fulfilled)
    {
        ShopOrderId = shopOrderId;
        Status = status;
        DeliveredAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Dòng hàng xuất giao
/// </summary>
public class FulfillmentItem : BaseEntity
{
    public Guid FulfillmentId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public int Quantity { get; private set; }

    // Navigation
    public Fulfillment? Fulfillment { get; private set; }
    public OrderItem? OrderItem { get; private set; }

    protected FulfillmentItem() { }

    public FulfillmentItem(Guid fulfillmentId, Guid orderItemId, int quantity)
    {
        FulfillmentId = fulfillmentId;
        OrderItemId = orderItemId;
        Quantity = quantity;
    }
}
