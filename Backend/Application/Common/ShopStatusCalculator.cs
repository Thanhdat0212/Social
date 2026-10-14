using Domain.Entities;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Application.Common;

/// <summary>
/// Tính toán trạng thái hiển thị hiệu lực (ShopEffectiveStatus) tại thời điểm đọc (runtime)
/// Phân tách rõ: ApprovalStatus (duyệt), OperatingStatus (mở/đóng), Sanction (phạt) và trạng thái tài khoản User.
/// </summary>
public static class ShopStatusCalculator
{
    public static ShopEffectiveStatus Compute(User user, Shop shop, bool hasActiveSanction)
    {
        if (shop.DeletedAtUtc != null)
            return ShopEffectiveStatus.Deleted;

        if (user.Status != UserStatus.Active)
            return ShopEffectiveStatus.UserUnavailable;

        if (shop.ApprovalStatus == ApprovalStatus.Pending)
            return ShopEffectiveStatus.PendingReview;

        if (shop.ApprovalStatus == ApprovalStatus.Rejected)
            return ShopEffectiveStatus.Rejected;

        if (hasActiveSanction)
            return ShopEffectiveStatus.Suspended;

        if (shop.OperatingStatus == OperatingStatus.Closed)
            return ShopEffectiveStatus.Closed;

        return ShopEffectiveStatus.Active;
    }
}
