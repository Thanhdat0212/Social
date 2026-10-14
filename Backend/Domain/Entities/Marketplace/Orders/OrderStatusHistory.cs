using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Orders;

/// <summary>
/// Lịch sử thay đổi trạng thái của đơn hàng (Audit Trail)
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public Guid ShopOrderId { get; private set; }
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public OrderActorType ActorType { get; private set; }
    public Guid? ActorId { get; private set; }
    public string? Reason { get; private set; }

    // Navigation
    public ShopOrder? ShopOrder { get; private set; }

    protected OrderStatusHistory() { }

    public OrderStatusHistory(
        Guid shopOrderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        OrderActorType actorType,
        Guid? actorId = null,
        string? reason = null)
    {
        ShopOrderId = shopOrderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActorType = actorType;
        ActorId = actorId;
        Reason = reason?.Trim();
    }
}
