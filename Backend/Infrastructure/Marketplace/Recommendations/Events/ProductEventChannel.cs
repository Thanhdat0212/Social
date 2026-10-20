using System.Threading.Channels;
using Application.Interfaces.Marketplace.Recommendations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Events;

/// <summary>
/// Triển khai hàng đợi sự kiện tương tác trong bộ nhớ RAM (In-Memory Bounded Channel)
/// Đảm bảo tính phi khóa (lock-free), an toàn đa luồng và chặn đứng nguy cơ quá tải database
/// </summary>
public class ProductEventChannel : IProductEventChannel
{
    private readonly Channel<ProductEventMessage> _channel;
    private readonly ILogger<ProductEventChannel> _logger;

    public ProductEventChannel(ILogger<ProductEventChannel> logger)
    {
        _logger = logger;

        // Giới hạn tối đa 20.000 items trong RAM (~3.5MB).
        // Nếu hàng đợi bị quá tải, tự động loại bỏ message cũ nhất (DropOldest) để ưu tiên hành vi mới nhất và bảo vệ RAM server.
        var options = new BoundedChannelOptions(20000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,  // Chỉ có 1 Background Consumer Worker đọc
            SingleWriter = false  // Nhiều request từ API controller có thể đẩy vào đồng thời
        };

        _channel = Channel.CreateBounded<ProductEventMessage>(options);
    }

    public ValueTask EnqueueEventAsync(ProductEventMessage message, CancellationToken cancellationToken = default)
    {
        // Thử ghi nhanh vào RAM (non-blocking)
        if (_channel.Writer.TryWrite(message))
        {
            return ValueTask.CompletedTask;
        }

        // Nếu buffer tạm thời đầy trong tích tắc, ghi bất đồng bộ
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<ProductEventMessage> ReadAllEventsAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
