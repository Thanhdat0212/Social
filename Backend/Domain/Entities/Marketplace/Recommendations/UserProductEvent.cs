using Domain.Common;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Recommendations;

/// <summary>
/// Thực thể ghi nhận hành vi tương tác của người dùng với sản phẩm trên Marketplace
/// Được lưu trữ bất đồng bộ thông qua hàng đợi in-memory Channel
/// </summary>
public class UserProductEvent : BaseEntity
{
    /// <summary>
    /// ID người dùng thực hiện hành vi (nullable nếu là khách vãng lai)
    /// </summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Mã phiên làm việc trên trình duyệt (dùng để gom nhóm hành vi của khách chưa đăng nhập)
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// ID sản phẩm được tương tác
    /// </summary>
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Loại hành vi tương tác (View, SearchClick, Wishlist, AddToCart, Purchase)
    /// </summary>
    public ProductEventType EventType { get; set; }

    /// <summary>
    /// Thời gian nán lại xem trang chi tiết (tính bằng giây, hỗ trợ implicit feedback)
    /// </summary>
    public int? DwellTimeSeconds { get; set; }
}
