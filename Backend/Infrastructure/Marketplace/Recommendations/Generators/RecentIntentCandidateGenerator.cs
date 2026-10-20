using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Generators;

/// <summary>
/// Generator sinh ứng viên dựa trên Ý định gần đây (Recent Intent):
/// 1. Sản phẩm xem gần nhất (trong 24h - 48h) có áp dụng Time-decay
/// 2. Từ khóa tìm kiếm gần đây từ SearchHistory
/// </summary>
public class RecentIntentCandidateGenerator
{
    private readonly SocialDbContext _context;
    private readonly ILogger<RecentIntentCandidateGenerator> _logger;

    public RecentIntentCandidateGenerator(
        SocialDbContext context,
        ILogger<RecentIntentCandidateGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketplaceCandidateItem>> GenerateCandidatesAsync(
        Guid? userId,
        string? sessionId,
        int quota = 50,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<MarketplaceCandidateItem>();
        var nowUtc = DateTime.UtcNow;
        var recentCutoff = nowUtc.AddHours(-48); // Chỉ lấy hành vi trong 48h qua

        try
        {
            // 1. Lấy danh sách ID sản phẩm người dùng vừa xem hoặc tương tác gần đây
            // Khớp index IX_UserProductEvents_UserId_CreatedAtUtc hoặc IX_UserProductEvents_SessionId_CreatedAtUtc
            List<Guid> recentProductIds = new();

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                recentProductIds = await _context.UserProductEvents
                    .AsNoTracking()
                    .Where(e => e.UserId == userId.Value && e.CreatedAtUtc >= recentCutoff)
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .Select(e => e.ProductId)
                    .Distinct()
                    .Take(10)
                    .ToListAsync(cancellationToken);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                recentProductIds = await _context.UserProductEvents
                    .AsNoTracking()
                    .Where(e => e.SessionId == sessionId && e.CreatedAtUtc >= recentCutoff)
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .Select(e => e.ProductId)
                    .Distinct()
                    .Take(10)
                    .ToListAsync(cancellationToken);
            }

            // Nếu có sản phẩm xem gần đây -> Tìm danh mục của các sản phẩm đó để gợi ý các sản phẩm cùng danh mục
            if (recentProductIds.Count > 0)
            {
                var relatedCategoryIds = await _context.Products
                    .AsNoTracking()
                    .Where(p => recentProductIds.Contains(p.Id) && p.CategoryId.HasValue)
                    .Select(p => p.CategoryId!.Value)
                    .Distinct()
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (relatedCategoryIds.Count > 0)
                {
                    var productsInSameCategory = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Shop)
                        .Include(p => p.Category)
                        .Include(p => p.Images.Where(i => i.IsCover))
                        .Where(p => p.CategoryId.HasValue && relatedCategoryIds.Contains(p.CategoryId.Value) &&
                                    !recentProductIds.Contains(p.Id) && // Không gợi ý lại đúng món vừa xem
                                    p.DeletedAtUtc == null &&
                                    p.Status == ProductStatus.Listed &&
                                    !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                                    p.Shop != null && p.Shop.DeletedAtUtc == null &&
                                    p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                                    p.Shop.OperatingStatus == OperatingStatus.Open &&
                                    !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
                        .OrderByDescending(p => p.RatingAverage)
                        .ThenByDescending(p => p.ReviewCount)
                        .Take(quota / 2)
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
                            BaseScore = 1.35,
                            Strategy = "RecentIntent",
                            Reason = "Tương tự sản phẩm bạn vừa xem"
                        })
                        .ToListAsync(cancellationToken);

                    candidates.AddRange(productsInSameCategory);
                }
            }

            // 2. Lấy từ khóa tìm kiếm gần nhất từ SearchHistory
            // Khớp index IX_SearchHistory_UserId_CreatedAtUtc
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                var recentSearches = await _context.SearchHistories
                    .AsNoTracking()
                    .Where(s => s.UserId == userId.Value)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .Select(s => s.NormalizedQuery)
                    .Distinct()
                    .Take(3)
                    .ToListAsync(cancellationToken);

                foreach (var query in recentSearches)
                {
                    if (string.IsNullOrWhiteSpace(query)) continue;

                    var searchMatches = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Shop)
                        .Include(p => p.Category)
                        .Include(p => p.Images.Where(i => i.IsCover))
                        .Where(p => EF.Functions.ILike(p.Name, $"%{query}%") &&
                                    p.DeletedAtUtc == null &&
                                    p.Status == ProductStatus.Listed &&
                                    !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                                    p.Shop != null && p.Shop.DeletedAtUtc == null &&
                                    p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                                    p.Shop.OperatingStatus == OperatingStatus.Open &&
                                    !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
                        .OrderByDescending(p => p.RatingAverage)
                        .Take(10)
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
                            BaseScore = 1.25,
                            Strategy = "RecentIntent",
                            Reason = $"Phù hợp với tìm kiếm \"{query}\""
                        })
                        .ToListAsync(cancellationToken);

                    candidates.AddRange(searchMatches);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong RecentIntentCandidateGenerator cho User {UserId}.", userId);
        }

        return candidates;
    }
}
