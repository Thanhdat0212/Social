using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Shops;

/// <summary>
/// Quyết định xử phạt / chế tài áp dụng lên gian hàng vi phạm
/// </summary>
public class ShopSanction : BaseEntity
{
    public Guid ShopId { get; private set; }
    public ShopSanctionType Type { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime StartAtUtc { get; private set; }
    public DateTime? EndAtUtc { get; private set; }
    public Guid CreatedByAdminId { get; private set; }
    public DateTime? LiftedAtUtc { get; private set; }
    public Guid? LiftedByAdminId { get; private set; }

    // Navigation property
    public Shop? Shop { get; private set; }

    protected ShopSanction() { }

    public ShopSanction(
        Guid shopId,
        ShopSanctionType type,
        string reason,
        Guid createdByAdminId,
        DateTime startAtUtc,
        DateTime? endAtUtc = null)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (createdByAdminId == Guid.Empty) throw new ArgumentException("CreatedByAdminId không hợp lệ.", nameof(createdByAdminId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Lý do xử phạt không được để trống.", nameof(reason));

        ShopId = shopId;
        Type = type;
        Reason = reason.Trim();
        CreatedByAdminId = createdByAdminId;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
    }

    /// <summary>
    /// Kiểm tra chế tài có đang còn hiệu lực tại thời điểm hiện tại hay không
    /// </summary>
    public bool IsActive(DateTime nowUtc)
    {
        return LiftedAtUtc == null &&
               nowUtc >= StartAtUtc &&
               (EndAtUtc == null || EndAtUtc.Value > nowUtc);
    }

    /// <summary>
    /// Admin gỡ bỏ chế tài xử phạt
    /// </summary>
    public void Lift(Guid adminId)
    {
        LiftedAtUtc = DateTime.UtcNow;
        LiftedByAdminId = adminId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
