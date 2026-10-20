using Domain.Common;

namespace Domain.Entities.Marketplace.Recommendations;

/// <summary>
/// Thực thể ghi nhận lịch sử tìm kiếm sản phẩm của người dùng trên Marketplace
/// Phục vụ cho tính năng gợi ý từ khóa gần đây và thuật toán gợi ý theo nhu cầu tìm kiếm (Recent Intent)
/// </summary>
public class SearchHistory : BaseEntity
{
    /// <summary>
    /// ID người dùng thực hiện tìm kiếm (nullable nếu là khách vãng lai)
    /// </summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Nội dung tìm kiếm gốc do người dùng nhập
    /// </summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung tìm kiếm đã được chuẩn hóa (viết thường, cắt khoảng trắng dư thừa)
    /// </summary>
    public string NormalizedQuery { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng sản phẩm tìm thấy tại thời điểm truy vấn
    /// </summary>
    public int ResultCount { get; set; } = 0;
}
