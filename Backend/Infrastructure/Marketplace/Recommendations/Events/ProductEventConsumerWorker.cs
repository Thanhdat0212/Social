using Application.Interfaces.Marketplace.Recommendations;
using Domain.Entities.Marketplace.Recommendations;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Events;

/// <summary>
/// Background Service tiêu thụ các sự kiện tương tác từ Channel
/// Gom batch 100 items hoặc mỗi 5 giây để ghi một lần duy nhất xuống PostgreSQL, giảm thiểu tối đa tải I/O DB
/// </summary>
public class ProductEventConsumerWorker : BackgroundService
{
    private readonly IProductEventChannel _eventChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductEventConsumerWorker> _logger;

    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);

    public ProductEventConsumerWorker(
        IProductEventChannel eventChannel,
        IServiceScopeFactory scopeFactory,
        ILogger<ProductEventConsumerWorker> logger)
    {
        _eventChannel = eventChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProductEventConsumerWorker đã khởi động, bắt đầu lắng nghe hàng đợi sự kiện.");

        var batch = new List<UserProductEvent>(BatchSize);
        var lastFlushTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Sử dụng luồng đọc liên tục từ Channel
                await foreach (var message in _eventChannel.ReadAllEventsAsync(stoppingToken))
                {
                    batch.Add(new UserProductEvent
                    {
                        UserId = message.UserId,
                        SessionId = message.SessionId,
                        ProductId = message.ProductId,
                        EventType = message.EventType,
                        DwellTimeSeconds = message.DwellTimeSeconds,
                        CreatedAtUtc = message.CreatedAtUtc
                    });

                    var timeSinceLastFlush = DateTime.UtcNow - lastFlushTime;

                    if (batch.Count >= BatchSize || (batch.Count > 0 && timeSinceLastFlush >= FlushInterval))
                    {
                        await FlushBatchAsync(batch, stoppingToken);
                        lastFlushTime = DateTime.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown an toàn
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp tiêu thụ sự kiện ProductEventConsumerWorker. Đang tiếp tục thử lại sau 2 giây...");
                await Task.Delay(2000, stoppingToken);
            }
        }

        // Flush những items còn lại trước khi dừng ứng dụng hoàn toàn
        if (batch.Count > 0)
        {
            try
            {
                await FlushBatchAsync(batch, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi flush mẻ sự kiện cuối cùng trước khi dừng worker.");
            }
        }

        _logger.LogInformation("ProductEventConsumerWorker đã dừng an toàn.");
    }

    private async Task FlushBatchAsync(List<UserProductEvent> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SocialDbContext>();

            await dbContext.UserProductEvents.AddRangeAsync(batch, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Đã flush thành công mẻ {Count} sự kiện sản phẩm xuống PostgreSQL.", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu mẻ {Count} sự kiện sản phẩm vào Database.", batch.Count);
        }
        finally
        {
            batch.Clear();
        }
    }
}
