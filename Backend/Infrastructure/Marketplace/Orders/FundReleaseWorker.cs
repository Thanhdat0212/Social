using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Repositories;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Orders;

/// <summary>
/// Worker chạy nền tự động quét các đơn hàng đã hết cửa sổ khiếu nại (48h),
/// chuyển trạng thái đơn hàng sang Completed và giải phóng tiền tạm giữ sang số dư khả dụng (Pending -> Available)
/// </summary>
public class FundReleaseWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FundReleaseWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);

    public FundReleaseWorker(
        IServiceProvider serviceProvider,
        ILogger<FundReleaseWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FundReleaseWorker bắt đầu hoạt động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEligibleOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét giải phóng tiền đơn hàng.");
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

        _logger.LogInformation("FundReleaseWorker dừng hoạt động.");
    }

    private async Task ProcessEligibleOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;

        // Tìm các đơn hàng Delivered đã qua thời hạn khiếu nại
        var matureOrders = await dbContext.ShopOrders
            .Where(o => o.Status == OrderStatus.Delivered &&
                        o.ComplaintWindowEndsAtUtc.HasValue &&
                        o.ComplaintWindowEndsAtUtc.Value <= now)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (matureOrders.Count == 0) return;

        _logger.LogInformation("Phát hiện {Count} đơn hàng đã hết hạn khiếu nại, bắt đầu hoàn tất và giải phóng tiền.", matureOrders.Count);

        foreach (var order in matureOrders)
        {
            // Kiểm tra bảo vệ: Nếu đơn hàng đang có khiếu nại mở thì không hoàn tất và không giải phóng tiền
            var hasActiveComplaints = await unitOfWork.Complaints.HasActiveComplaintsForOrderAsync(order.Id, cancellationToken);
            if (hasActiveComplaints)
            {
                _logger.LogInformation("Đơn hàng {OrderNo} đang có khiếu nại mở, tạm giữ tiền ở ShopPending.", order.OrderNo);
                continue;
            }

            try
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);

                order.MarkCompleted();
                await unitOfWork.SaveChangesAsync(cancellationToken);

                if (order.Source == OrderSource.Marketplace)
                {
                    await ledgerService.ReleaseOrderFundsAsync(order.Id, cancellationToken);
                }

                await unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Đã hoàn tất đơn hàng {OrderNo} và giải phóng tiền thành công cho Shop {ShopId}.", order.OrderNo, order.ShopId);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi giải phóng tiền cho đơn hàng {OrderId}.", order.Id);
            }
        }
    }
}
