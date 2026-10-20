using Domain.Entities.Marketplace.Recommendations;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Jobs;

/// <summary>
/// Background Job chạy định kỳ phân tích các đơn hàng đã thanh toán thành công
/// Tính toán trước ma trận tương quan giữa các cặp sản phẩm (Co-occurrence / Jaccard Similarity)
/// Phục vụ cho tính năng "Thường được mua cùng nhau" (Frequently Bought Together)
/// </summary>
public class ProductCoOccurrenceWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductCoOccurrenceWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12); // Chạy 2 lần/ngày

    public ProductCoOccurrenceWorker(
        IServiceProvider serviceProvider,
        ILogger<ProductCoOccurrenceWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProductCoOccurrenceWorker đã khởi động.");

        // Chờ 45 giây sau khi ứng dụng start để hệ thống ổn định
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCoOccurrenceMiningAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình tính toán cặp sản phẩm mua kèm định kỳ.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCoOccurrenceMiningAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bắt đầu phân tích lịch sử đơn hàng để tính toán cặp sản phẩm mua kèm...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SocialDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-90);

        // 1. Lấy danh sách các sản phẩm trong các đơn hàng đã giao hoặc hoàn tất trong 90 ngày qua
        var orders = await dbContext.ShopOrders
            .AsNoTracking()
            .Where(o => (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed) && o.CreatedAtUtc >= cutoffDate)
            .Select(o => o.Items.Select(i => i.ProductId).Distinct().ToList())
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            _logger.LogInformation("Chưa có đủ đơn hàng trong 90 ngày qua để tính toán cặp sản phẩm mua kèm.");
            return;
        }

        // Đếm số lần mỗi sản phẩm xuất hiện trong các đơn
        var singleCounts = new Dictionary<Guid, int>();
        // Đếm số lần cặp (A, B) cùng xuất hiện
        var pairCounts = new Dictionary<(Guid ProductA, Guid ProductB), int>();

        foreach (var productIds in orders)
        {
            foreach (var pid in productIds)
            {
                singleCounts[pid] = singleCounts.TryGetValue(pid, out int sCount) ? sCount + 1 : 1;
            }

            // Nếu đơn hàng có từ 2 sản phẩm khác nhau trở lên
            if (productIds.Count >= 2)
            {
                for (int i = 0; i < productIds.Count; i++)
                {
                    for (int j = 0; j < productIds.Count; j++)
                    {
                        if (i == j) continue;

                        (Guid ProductA, Guid ProductB) pair = (productIds[i], productIds[j]);
                        pairCounts[pair] = pairCounts.TryGetValue(pair, out int pCount) ? pCount + 1 : 1;
                    }
                }
            }
        }

        if (pairCounts.Count == 0)
        {
            _logger.LogInformation("Các đơn hàng hiện tại chỉ có 1 sản phẩm đơn lẻ. Bỏ qua cập nhật bảng đệm.");
            return;
        }

        // 2. Tính hệ số tương đồng Jaccard cho từng cặp
        // Jaccard(A, B) = Count(A, B) / (Count(A) + Count(B) - Count(A, B))
        var nowUtc = DateTime.UtcNow;
        var coOccurrenceRecords = new List<ProductCoOccurrence>();

        // Nhóm theo ProductA để lấy Top 10 sản phẩm B có điểm số cao nhất cho mỗi A
        var groupedByA = pairCounts
            .GroupBy(p => p.Key.ProductA)
            .ToList();

        foreach (var group in groupedByA)
        {
            var productA = group.Key;
            int countA = singleCounts.TryGetValue(productA, out int cA) ? cA : 1;

            var topPairs = group
                .Select(p =>
                {
                    var productB = p.Key.ProductB;
                    int countB = singleCounts.TryGetValue(productB, out int cB) ? cB : 1;
                    int countAB = p.Value;

                    int union = countA + countB - countAB;
                    double score = union > 0 ? (double)countAB / union : 0.0;

                    return new ProductCoOccurrence
                    {
                        ProductAId = productA,
                        ProductBId = productB,
                        CoOccurrenceCount = countAB,
                        Score = Math.Round(score, 4),
                        UpdatedAtUtc = nowUtc
                    };
                })
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.CoOccurrenceCount)
                .Take(10) // Lưu tối đa 10 sản phẩm mua kèm tốt nhất cho mỗi sản phẩm
                .ToList();

            coOccurrenceRecords.AddRange(topPairs);
        }

        // 3. Cập nhật vào Database (Xóa dữ liệu cũ và ghi đè danh sách mới nhất)
        await dbContext.ProductCoOccurrences.ExecuteDeleteAsync(cancellationToken);
        await dbContext.ProductCoOccurrences.AddRangeAsync(coOccurrenceRecords, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã hoàn tất tính toán và lưu thành công {Count} cặp sản phẩm mua kèm vào ProductCoOccurrences.", coOccurrenceRecords.Count);
    }
}
