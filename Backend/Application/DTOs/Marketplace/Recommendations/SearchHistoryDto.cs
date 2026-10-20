namespace Application.DTOs.Marketplace.Recommendations;

/// <summary>
/// DTO trả về lịch sử tìm kiếm gần đây để hiển thị popup dropdown trên thanh Search Marketplace
/// </summary>
public class SearchHistoryDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Nội dung từ khóa đã tìm kiếm
    /// </summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng sản phẩm tìm thấy tại thời điểm đó
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// Thời điểm tìm kiếm
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// DTO gửi lên để lưu một từ khóa tìm kiếm mới
/// </summary>
public class RecordSearchQueryRequest
{
    public string QueryText { get; set; } = string.Empty;
    public int ResultCount { get; set; } = 0;
    public string? SessionId { get; set; }
}
