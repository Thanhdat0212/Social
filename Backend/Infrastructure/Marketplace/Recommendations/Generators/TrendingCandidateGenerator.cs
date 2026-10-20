using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Generators;

/// <summary>
/// Generator sinh ứng viên Thịnh hành (Trending / Popularity / High Rating):
/// Đóng vai trò làm Fallback cốt lõi giải quyết bài toán Cold-Start cho người dùng mới
/// Hoặc khi người dùng chưa có bất kỳ lịch sử tương tác nào
/// </summary>
public class TrendingCandidateGenerator
{
    private readonly SocialDbContext _context;
    private readonly ILogger<TrendingCandidateGenerator> _logger;

    public TrendingCandidateGenerator(
        SocialDbContext context,
        ILogger<TrendingCandidateGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketplaceCandidateItem>> GenerateTrendingCandidatesAsync(
        Guid? categoryId = null,
        int quota = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = DateTime.UtcNow;

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.Images.Where(i => i.IsCover))
                .Where(p => p.DeletedAtUtc == null &&
                            p.Status == ProductStatus.Listed &&
                            !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                            p.Shop != null && p.Shop.DeletedAtUtc == null &&
                            p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                            p.Shop.OperatingStatus == OperatingStatus.Open &&
                            !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)));

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var trendingProducts = await query
                .OrderByDescending(p => p.RatingAverage)
                .ThenByDescending(p => p.ReviewCount)
                .ThenByDescending(p => p.CreatedAtUtc)
                .Take(quota)
                .Select(p => new MarketplaceCandidateItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ShopId = p.ShopId,
                    ShopName = p.Shop != null ? p.Shop.Name : string.Empty,
                    ShopSlug = p.Shop != null ? p.Shop.Slug : string.Empty,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    CoverImageUrl = p.Images.Select(i => i.Url).FirstOrDefault(),
                    MinPrice = p.MinPriceCache,
                    MaxPrice = p.MaxPriceCache,
                    RatingAverage = p.RatingAverage,
                    ReviewCount = p.ReviewCount,
                    CreatedAtUtc = p.CreatedAtUtc,
                    BaseScore = 0.9,
                    Strategy = "Trending",
                    Reason = p.RatingAverage >= 4.5 ? "Đánh giá xuất sắc trên sàn" : "Sản phẩm được quan tâm nhiều"
                })
                .ToListAsync(cancellationToken);

            return trendingProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong TrendingCandidateGenerator.");
            return Array.Empty<MarketplaceCandidateItem>();
        }
    }
}
