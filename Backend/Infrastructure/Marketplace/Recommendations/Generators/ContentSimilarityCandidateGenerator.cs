using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Generators;

/// <summary>
/// Generator sinh ứng viên dựa trên Độ tương đồng nội dung (Content-Based Similarity):
/// Phục vụ cho mục "Sản phẩm tương tự" trên trang chi tiết sản phẩm:
/// - Cùng danh mục (CategoryId)
/// - Cùng khoảng giá (Biên độ +-35%)
/// - Loại trừ chính sản phẩm đang xem
/// </summary>
public class ContentSimilarityCandidateGenerator
{
    private readonly SocialDbContext _context;
    private readonly ILogger<ContentSimilarityCandidateGenerator> _logger;

    public ContentSimilarityCandidateGenerator(
        SocialDbContext context,
        ILogger<ContentSimilarityCandidateGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketplaceCandidateItem>> GenerateRelatedProductsAsync(
        Guid productId,
        int quota = 8,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseProduct = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => new { p.Id, p.CategoryId, p.MinPriceCache, p.MaxPriceCache })
                .FirstOrDefaultAsync(cancellationToken);

            if (baseProduct == null || !baseProduct.CategoryId.HasValue)
            {
                return Array.Empty<MarketplaceCandidateItem>();
            }

            var nowUtc = DateTime.UtcNow;
            long minBound = (long)(baseProduct.MinPriceCache * 0.65);
            long maxBound = (long)(baseProduct.MaxPriceCache * 1.35);

            // 1. Thử tìm các sản phẩm cùng danh mục và cùng khoảng giá
            var related = await _context.Products
                .AsNoTracking()
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.Images.Where(i => i.IsCover))
                .Where(p => p.Id != productId &&
                            p.CategoryId == baseProduct.CategoryId.Value &&
                            p.DeletedAtUtc == null &&
                            p.Status == ProductStatus.Listed &&
                            !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                            p.Shop != null && p.Shop.DeletedAtUtc == null &&
                            p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                            p.Shop.OperatingStatus == OperatingStatus.Open &&
                            !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
                .Where(p => (minBound == 0 && maxBound == 0) || (p.MinPriceCache >= minBound && p.MinPriceCache <= maxBound))
                .OrderByDescending(p => p.RatingAverage)
                .ThenByDescending(p => p.ReviewCount)
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
                    BaseScore = 1.0,
                    Strategy = "ContentSimilarity",
                    Reason = "Sản phẩm tương tự cùng phân khúc"
                })
                .ToListAsync(cancellationToken);

            // 2. Nếu không đủ quota do khoảng giá quá hẹp, nới lỏng điều kiện khoảng giá
            if (related.Count < quota)
            {
                var existingIds = related.Select(r => r.Id).ToList();
                existingIds.Add(productId);

                int remaining = quota - related.Count;

                var fallback = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.Category)
                    .Include(p => p.Images.Where(i => i.IsCover))
                    .Where(p => !existingIds.Contains(p.Id) &&
                                p.CategoryId == baseProduct.CategoryId.Value &&
                                p.DeletedAtUtc == null &&
                                p.Status == ProductStatus.Listed &&
                                !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                                p.Shop != null && p.Shop.DeletedAtUtc == null &&
                                p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                                p.Shop.OperatingStatus == OperatingStatus.Open &&
                                !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
                    .OrderByDescending(p => p.RatingAverage)
                    .Take(remaining)
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
                        BaseScore = 0.85,
                        Strategy = "ContentSimilarity",
                        Reason = "Cùng danh mục đang xem"
                    })
                    .ToListAsync(cancellationToken);

                related.AddRange(fallback);
            }

            return related;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh sản phẩm tương tự cho ProductId {ProductId}.", productId);
            return Array.Empty<MarketplaceCandidateItem>();
        }
    }
}
