using Application.Interfaces.Marketplace.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Audit;

public class DailyReconciliationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyReconciliationWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public DailyReconciliationWorker(
        IServiceProvider serviceProvider,
        ILogger<DailyReconciliationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyReconciliationWorker đã khởi động.");

        // Chờ 30 giây sau khi ứng dụng start để hệ thống ổn định
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

                var summary = await reconciliationService.RunReconciliationAsync(stoppingToken);

                if (!summary.IsSystemHealthy)
                {
                    _logger.LogWarning("Phát hiện bất thường trong đối soát sổ cái! Số shop bị lệch: {Count}. Tự động kích hoạt cơ chế tự phục hồi (Self-healing)...",
                        summary.MismatchedShops.Count);

                    foreach (var mismatched in summary.MismatchedShops)
                    {
                        try
                        {
                            await reconciliationService.ReconcileAndFixShopBalanceAsync(mismatched.ShopId, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi tự động sửa số dư cho Shop {ShopId}.", mismatched.ShopId);
                        }
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình chạy đối soát định kỳ.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
