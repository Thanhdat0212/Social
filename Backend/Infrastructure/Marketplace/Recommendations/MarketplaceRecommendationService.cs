using Application.DTOs.Marketplace.Recommendations;
using Application.Interfaces.Marketplace.Recommendations;
using Domain.Enums;
using Infrastructure.Marketplace.Recommendations.Generators;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations;

/// <summary>
/// Service điều phối chính (Orchestrator) của Hệ thống Đề xuất sản phẩm Marketplace
/// Thực hiện quy trình 4 tầng:
/// 1. Sinh ứng viên đa nguồn (Social Affinity + Recent Intent + Trending)
/// 2. Lọc bỏ hàng số đã mua (Negative Filtering) & hàng vi phạm
/// 3. Chấm điểm đa trọng số trong RAM (In-Memory Scoring)
/// 4. Đa dạng hóa danh mục/shop (Diversity Re-ranking) và Memory Cache
/// </summary>
public class MarketplaceRecommendationService : IMarketplaceRecommendationService
{
    private readonly SocialDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly SocialAffinityCandidateGenerator _socialAffinity;
    private readonly RecentIntentCandidateGenerator _recentIntent;
    private readonly ContentSimilarityCandidateGenerator _contentSimilarity;
    private readonly TrendingCandidateGenerator _trending;
    private readonly ILogger<MarketplaceRecommendationService> _logger;

    public MarketplaceRecommendationService(
        SocialDbContext context,
        IMemoryCache memoryCache,
        SocialAffinityCandidateGenerator socialAffinity,
        RecentIntentCandidateGenerator recentIntent,
        ContentSimilarityCandidateGenerator contentSimilarity,
        TrendingCandidateGenerator trending,
        ILogger<MarketplaceRecommendationService> logger)
    {
        _context = context;
        _memoryCache = memoryCache;
        _socialAffinity = socialAffinity;
        _recentIntent = recentIntent;
        _contentSimilarity = contentSimilarity;
        _trending = trending;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecommendationProductCardDto>> GetForYouProductsAsync(
        Guid? userId,
        string? sessionId,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rec:user:{(userId.HasValue ? userId.Value.ToString() : sessionId ?? "guest")}:for-you";

        // 1. Kiểm tra Memory Cache trước (Cache Hit -> phản hồi trong < 2ms)
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<RecommendationProductCardDto>? cached) && cached != null)
        {
            return cached;
        }

        // 2. Tầng Candidate Generation: Gom ứng viên từ các nguồn
        var candidatesMap = new Dictionary<Guid, MarketplaceCandidateItem>();

        // Nguồn 1: Sở thích MXH & Shop Follows
        if (userId.HasValue)
        {
            var socialCandidates = await _socialAffinity.GenerateCandidatesAsync(userId, 50, cancellationToken);
            foreach (var item in socialCandidates)
            {
                candidatesMap[item.Id] = item;
            }
        }

        // Nguồn 2: Hành vi xem gần đây & từ khóa tìm kiếm
        var intentCandidates = await _recentIntent.GenerateCandidatesAsync(userId, sessionId, 40, cancellationToken);
        foreach (var item in intentCandidates)
        {
            if (!candidatesMap.TryGetValue(item.Id, out var existing) || item.BaseScore > existing.BaseScore)
            {
                candidatesMap[item.Id] = item;
            }
        }

        // Nguồn 3: Trending / Đánh giá cao (Fallback chống Cold-start)
        var trendingCandidates = await _trending.GenerateTrendingCandidatesAsync(null, 30, cancellationToken);
        foreach (var item in trendingCandidates)
        {
            if (!candidatesMap.ContainsKey(item.Id))
            {
                candidatesMap[item.Id] = item;
            }
        }

        if (candidatesMap.Count == 0)
        {
            return Array.Empty<RecommendationProductCardDto>();
        }

        var candidateList = candidatesMap.Values.ToList();

        // 3. Tầng Negative Filtering: Loại bỏ sản phẩm số người dùng ĐÃ MUA thành công
        if (userId.HasValue)
        {
            var purchasedProductIds = await _context.ShopOrders
                .AsNoTracking()
                .Where(o => o.BuyerUserId == userId.Value && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                .SelectMany(o => o.Items.Select(i => i.ProductId))
                .Distinct()
                .ToListAsync(cancellationToken);

            if (purchasedProductIds.Count > 0)
            {
                var purchasedSet = new HashSet<Guid>(purchasedProductIds);
                candidateList.RemoveAll(c => purchasedSet.Contains(c.Id));
            }
        }

        // 4. Tầng In-Memory Scoring: Chấm điểm đa yếu tố trên RAM (CPU computation < 0.1ms)
        var scoredItems = candidateList
            .Select(c =>
            {
                // Công thức tính điểm kết hợp:
                // BaseScore (tín hiệu nguồn) + RatingAverage chuẩn hóa + ReviewCount chuẩn hóa
                double ratingPart = (c.RatingAverage / 5.0) * 0.30;
                double popularityPart = Math.Min(1.0, c.ReviewCount / 30.0) * 0.30;
                double signalPart = c.BaseScore * 0.40;

                double finalScore = Math.Round(signalPart + ratingPart + popularityPart, 4);

                return new
                {
                    Item = c,
                    Score = finalScore
                };
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        // 5. Tầng Diversity Re-Ranking: Đa dạng hóa danh mục & shop, chống "Bong bóng đơn điệu"
        var selected = new List<RecommendationProductCardDto>();
        var categoryCounts = new Dictionary<Guid, int>();
        var shopCounts = new Dictionary<Guid, int>();

        foreach (var entry in scoredItems)
        {
            var item = entry.Item;
            var catId = item.CategoryId ?? Guid.Empty;
            var shopId = item.ShopId;

            int currentCatCount = categoryCounts.GetValueOrDefault(catId, 0);
            int currentShopCount = shopCounts.GetValueOrDefault(shopId, 0);

            // Quy tắc đa dạng hóa: Không quá 3 sản phẩm cùng danh mục, không quá 2 sản phẩm từ 1 shop
            if (currentCatCount >= 3 || currentShopCount >= 2)
            {
                continue;
            }

            selected.Add(new RecommendationProductCardDto
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                ShopId = item.ShopId,
                ShopName = item.ShopName,
                ShopSlug = item.ShopSlug,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                CoverImageUrl = item.CoverImageUrl,
                MinPrice = item.MinPrice,
                MaxPrice = item.MaxPrice,
                RatingAverage = item.RatingAverage,
                ReviewCount = item.ReviewCount,
                CreatedAtUtc = item.CreatedAtUtc,
                RecommendationReason = item.Reason,
                RecommendationScore = entry.Score,
                Strategy = item.Strategy
            });

            categoryCounts[catId] = currentCatCount + 1;
            shopCounts[shopId] = currentShopCount + 1;

            if (selected.Count >= limit)
            {
                break;
            }
        }

        // Nếu qua bộ lọc đa dạng hóa mà chưa đủ limit, bù đắp từ các sản phẩm còn lại
        if (selected.Count < limit)
        {
            var selectedIds = new HashSet<Guid>(selected.Select(s => s.Id));
            foreach (var entry in scoredItems)
            {
                if (!selectedIds.Contains(entry.Item.Id))
                {
                    var item = entry.Item;
                    selected.Add(new RecommendationProductCardDto
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Slug = item.Slug,
                        ShopId = item.ShopId,
                        ShopName = item.ShopName,
                        ShopSlug = item.ShopSlug,
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        CoverImageUrl = item.CoverImageUrl,
                        MinPrice = item.MinPrice,
                        MaxPrice = item.MaxPrice,
                        RatingAverage = item.RatingAverage,
                        ReviewCount = item.ReviewCount,
                        CreatedAtUtc = item.CreatedAtUtc,
                        RecommendationReason = item.Reason,
                        RecommendationScore = entry.Score,
                        Strategy = item.Strategy
                    });

                    if (selected.Count >= limit) break;
                }
            }
        }

        // 6. Lưu vào Memory Cache với TTL 5 phút
        _memoryCache.Set(cacheKey, selected, TimeSpan.FromMinutes(5));

        return selected;
    }

    public async Task<IReadOnlyList<RecommendationProductCardDto>> GetRelatedProductsAsync(
        Guid productId,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rec:prod:{productId}:related";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<RecommendationProductCardDto>? cached) && cached != null)
        {
            return cached;
        }

        var relatedCandidates = await _contentSimilarity.GenerateRelatedProductsAsync(productId, limit, cancellationToken);

        var result = relatedCandidates.Select(c => new RecommendationProductCardDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ShopId = c.ShopId,
            ShopName = c.ShopName,
            ShopSlug = c.ShopSlug,
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            CoverImageUrl = c.CoverImageUrl,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            RatingAverage = c.RatingAverage,
            ReviewCount = c.ReviewCount,
            CreatedAtUtc = c.CreatedAtUtc,
            RecommendationReason = c.Reason,
            RecommendationScore = c.BaseScore,
            Strategy = c.Strategy
        }).ToList();

        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }

    public async Task<IReadOnlyList<RecommendationProductCardDto>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit = 4,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rec:prod:{productId}:bought-together";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<RecommendationProductCardDto>? cached) && cached != null)
        {
            return cached;
        }

        // Đọc trực tiếp từ bảng đệm ProductCoOccurrences (Khớp index IX_ProductCoOccurrences_ProductAId_Score)
        var coOccurrences = await _context.ProductCoOccurrences
            .AsNoTracking()
            .Include(c => c.ProductB)
                .ThenInclude(p => p.Shop)
            .Include(c => c.ProductB)
                .ThenInclude(p => p.Category)
            .Include(c => c.ProductB)
                .ThenInclude(p => p.Images.Where(i => i.IsCover))
            .Where(c => c.ProductAId == productId &&
                        c.ProductB.DeletedAtUtc == null &&
                        c.ProductB.Status == ProductStatus.Listed &&
                        !c.ProductB.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                        c.ProductB.Shop != null && c.ProductB.Shop.DeletedAtUtc == null &&
                        c.ProductB.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                        c.ProductB.Shop.OperatingStatus == OperatingStatus.Open)
            .OrderByDescending(c => c.Score)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var result = coOccurrences.Select(c => new RecommendationProductCardDto
        {
            Id = c.ProductB.Id,
            Name = c.ProductB.Name,
            Slug = c.ProductB.Slug,
            ShopId = c.ProductB.ShopId,
            ShopName = c.ProductB.Shop != null ? c.ProductB.Shop.Name : string.Empty,
            ShopSlug = c.ProductB.Shop != null ? c.ProductB.Shop.Slug : string.Empty,
            CategoryId = c.ProductB.CategoryId,
            CategoryName = c.ProductB.Category != null ? c.ProductB.Category.Name : null,
            CoverImageUrl = c.ProductB.Images.Select(i => i.Url).FirstOrDefault(),
            MinPrice = c.ProductB.MinPriceCache,
            MaxPrice = c.ProductB.MaxPriceCache,
            RatingAverage = c.ProductB.RatingAverage,
            ReviewCount = c.ProductB.ReviewCount,
            CreatedAtUtc = c.ProductB.CreatedAtUtc,
            RecommendationReason = "Thường được mua cùng sản phẩm này",
            RecommendationScore = c.Score,
            Strategy = "BoughtTogether"
        }).ToList();

        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }

    public async Task<IReadOnlyList<RecommendationProductCardDto>> GetTrendingProductsAsync(
        Guid? categoryId,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rec:trending:{(categoryId.HasValue ? categoryId.Value.ToString() : "all")}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<RecommendationProductCardDto>? cached) && cached != null)
        {
            return cached;
        }

        var trendingCandidates = await _trending.GenerateTrendingCandidatesAsync(categoryId, limit, cancellationToken);

        var result = trendingCandidates.Select(c => new RecommendationProductCardDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ShopId = c.ShopId,
            ShopName = c.ShopName,
            ShopSlug = c.ShopSlug,
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            CoverImageUrl = c.CoverImageUrl,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            RatingAverage = c.RatingAverage,
            ReviewCount = c.ReviewCount,
            CreatedAtUtc = c.CreatedAtUtc,
            RecommendationReason = c.Reason,
            RecommendationScore = c.BaseScore,
            Strategy = c.Strategy
        }).ToList();

        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }

    public async Task RecordSearchQueryAsync(
        Guid? userId,
        RecordSearchQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.QueryText))
            return;

        var cleanQuery = request.QueryText.Trim();
        var normalized = cleanQuery.ToLowerInvariant();

        var entry = new Domain.Entities.Marketplace.Recommendations.SearchHistory
        {
            UserId = userId,
            QueryText = cleanQuery.Length > 200 ? cleanQuery[..200] : cleanQuery,
            NormalizedQuery = normalized.Length > 200 ? normalized[..200] : normalized,
            ResultCount = request.ResultCount
        };

        _context.SearchHistories.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHistoryDto>> GetRecentSearchesAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var rawSearches = await _context.SearchHistories
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(limit * 2)
            .Select(s => new SearchHistoryDto
            {
                Id = s.Id,
                QueryText = s.QueryText,
                ResultCount = s.ResultCount,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        // Nhóm các từ khóa giống nhau để hiển thị danh sách duy nhất
        var distinct = rawSearches
            .GroupBy(s => s.QueryText.ToLowerInvariant())
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return distinct;
    }
}

