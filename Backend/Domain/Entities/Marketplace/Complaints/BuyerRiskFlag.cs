using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Complaints;

/// <summary>
/// Gắn cờ rủi ro người mua có dấu hiệu lạm dụng khiếu nại hoặc trục lợi mã số
/// </summary>
public class BuyerRiskFlag : BaseEntity
{
    public Guid BuyerUserId { get; private set; }
    public RiskFlagType Type { get; private set; }
    public string Note { get; private set; } = string.Empty;

    // Navigation
    public User? BuyerUser { get; private set; }

    protected BuyerRiskFlag() { }

    public BuyerRiskFlag(Guid buyerUserId, RiskFlagType type, string note)
    {
        if (buyerUserId == Guid.Empty) throw new ArgumentException("BuyerUserId không hợp lệ.", nameof(buyerUserId));
        BuyerUserId = buyerUserId;
        Type = type;
        Note = string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();
    }
}
