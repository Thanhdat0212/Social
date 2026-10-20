namespace Domain.Enums;

/// <summary>
/// Các loại hành vi người dùng tương tác với sản phẩm trên Marketplace
/// Phục vụ cho hệ thống Event Tracking và Recommendation Engine
/// </summary>
public enum ProductEventType
{
    /// <summary>
    /// Người dùng xem chi tiết sản phẩm (chỉ tính khi Dwell Time >= 3 giây)
    /// </summary>
    View = 1,

    /// <summary>
    /// Người dùng bấm vào sản phẩm từ kết quả tìm kiếm hoặc bảng tin gợi ý
    /// </summary>
    SearchClick = 2,

    /// <summary>
    /// Người dùng thêm sản phẩm vào danh sách yêu thích (Wishlist)
    /// </summary>
    Wishlist = 3,

    /// <summary>
    /// Người dùng thêm sản phẩm vào giỏ hàng (Ý định mua cao)
    /// </summary>
    AddToCart = 4,

    /// <summary>
    /// Người dùng hoàn tất thanh toán mua sản phẩm thành công (Ghi nhận từ Backend)
    /// </summary>
    Purchase = 5
}
