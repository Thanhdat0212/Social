using Application.Interfaces.Repositories;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Complaints;

/// <summary>
/// Worker chạy nền tự động quét các khiếu nại quá hạn 24h mà Shop không phản hồi,
/// tự động chuyển trạng thái lên EscalatedToAdmin để Admin vào phân xử.
/// </summary>
public class ComplaintEscalationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComplaintEscalationWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public ComplaintEscalationWorker(
        IServiceProvider serviceProvider,
        ILogger<ComplaintEscalationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComplaintEscalationWorker bắt đầu hoạt động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueComplaintsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét khiếu nại quá hạn phản hồi của Shop.");
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

        _logger.LogInformation("ComplaintEscalationWorker dừng hoạt động.");
    }

    private async Task ProcessOverdueComplaintsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var overdueComplaints = await unitOfWork.Complaints.GetOverdueSubmittedComplaintsAsync(DateTime.UtcNow, cancellationToken);
        if (overdueComplaints.Count == 0) return;

        _logger.LogInformation("Phát hiện {Count} khiếu nại quá hạn 24h Shop không phản hồi, tự động chuyển Admin phân xử.", overdueComplaints.Count);

        foreach (var complaint in overdueComplaints)
        {
            try
            {
                complaint.EscalateToAdmin();
                unitOfWork.Complaints.Update(complaint);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Đã chuyển khiếu nại {ComplaintId} lên hàng đợi Admin thành công.", complaint.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi chuyển khiếu nại {ComplaintId} lên Admin.", complaint.Id);
            }
        }
    }
}
