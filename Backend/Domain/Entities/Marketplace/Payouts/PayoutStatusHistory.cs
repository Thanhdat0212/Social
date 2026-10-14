using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Payouts;

public class PayoutStatusHistory : BaseEntity
{
    public Guid PayoutId { get; private set; }
    public PayoutStatus PreviousStatus { get; private set; }
    public PayoutStatus NewStatus { get; private set; }
    public Guid? ActorId { get; private set; }
    public PayoutActorType ActorType { get; private set; }
    public string? Note { get; private set; }

    // Navigation
    public Payout? Payout { get; private set; }

    protected PayoutStatusHistory() { }

    public PayoutStatusHistory(
        Guid payoutId,
        PayoutStatus previousStatus,
        PayoutStatus newStatus,
        Guid? actorId,
        PayoutActorType actorType,
        string? note = null)
    {
        PayoutId = payoutId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ActorId = actorId;
        ActorType = actorType;
        Note = note;
    }
}
