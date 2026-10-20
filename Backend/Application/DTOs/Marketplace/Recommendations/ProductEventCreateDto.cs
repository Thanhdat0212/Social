using Domain.Enums;

namespace Application.DTOs.Marketplace.Recommendations;

/// <summary>
/// DTO tiếp nhận dữ liệu sự kiện tương tác của người dùng từ Frontend gửi lên API
/// </summary>
public class ProductEventCreateDto
{
    /// <summary>
    /// ID của sản phẩm được tương tác
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Loại hành vi tương tác (View, SearchClick, Wishlist, AddToCart)
    /// Lưu ý: Event Purchase chỉ được kích hoạt từ Backend
    /// </summary>
    public ProductEventType EventType { get; set; }

    /// <summary>
    /// Thời gian nán lại xem trang sản phẩm (tính bằng giây)
    /// </summary>
    public int? DwellTimeSeconds { get; set; }

    /// <summary>
    /// Mã phiên làm việc trên trình duyệt của khách chưa đăng nhập
    /// </summary>
    public string? SessionId { get; set; }
}
