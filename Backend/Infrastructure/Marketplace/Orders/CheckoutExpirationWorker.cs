using Application.Interfaces.Repositories;
using Application.Interfaces.Marketplace.Orders;
using Application.Interfaces.Repositories.Marketplace.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Orders;

/// <summary>
/// Worker chạy nền định kỳ quét và giải phóng các phiên Checkout quá hạn (15 phút),
/// trả lại mã kho từ Reserved -> Available.
/// </summary>
public class CheckoutExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CheckoutExpirationWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public CheckoutExpirationWorker(
        IServiceProvider serviceProvider,
        ILogger<CheckoutExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CheckoutExpirationWorker bắt đầu hoạt động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredCheckoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét Checkout hết hạn.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CheckoutExpirationWorker dừng hoạt động.");
    }

    private async Task ProcessExpiredCheckoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var checkoutService = scope.ServiceProvider.GetRequiredService<ICheckoutService>();

        var expiredCheckouts = await unitOfWork.Checkouts.GetExpiredOpenCheckoutsAsync(DateTime.UtcNow, cancellationToken);
        if (expiredCheckouts.Count == 0) return;

        _logger.LogInformation("Phát hiện {Count} phiên checkout hết hạn cần giải phóng mã.", expiredCheckouts.Count);

        foreach (var checkout in expiredCheckouts)
        {
            try
            {
                await checkoutService.ExpireCheckoutAsync(checkout.Id, cancellationToken);
                _logger.LogInformation("Đã giải phóng thành công phiên checkout {CheckoutNo} ({CheckoutId}).", checkout.CheckoutNo, checkout.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi giải phóng phiên checkout {CheckoutId}.", checkout.Id);
            }
        }
    }
}
