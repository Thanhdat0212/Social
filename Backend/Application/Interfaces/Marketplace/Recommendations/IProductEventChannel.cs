using Domain.Enums;

namespace Application.Interfaces.Marketplace.Recommendations;

/// <summary>
/// Cấu trúc thông điệp sự kiện nhẹ truyền trong hàng đợi RAM nội bộ
/// </summary>
public class ProductEventMessage
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public Guid ProductId { get; set; }
    public ProductEventType EventType { get; set; }
    public int? DwellTimeSeconds { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Giao diện hàng đợi bộ nhớ đệm bất đồng bộ (In-memory Non-blocking Channel)
/// Cho phép các controller/service tiếp nhận event và đẩy vào hàng đợi trong RAM mà không làm chậm DB
/// </summary>
public interface IProductEventChannel
{
    /// <summary>
    /// Đẩy một thông điệp sự kiện vào hàng đợi RAM (Thời gian thực thi < 0.05ms)
    /// </summary>
    ValueTask EnqueueEventAsync(ProductEventMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đọc luồng dữ liệu liên tục từ hàng đợi để Background Consumer Worker gom mẻ ghi xuống PostgreSQL
    /// </summary>
    IAsyncEnumerable<ProductEventMessage> ReadAllEventsAsync(CancellationToken cancellationToken = default);
}
