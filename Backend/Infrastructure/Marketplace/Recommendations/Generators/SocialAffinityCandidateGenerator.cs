using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Recommendations.Generators;

/// <summary>
/// Generator sinh ứng viên dựa trên liên kết Mạng Xã Hội (Social Affinity):
/// 1. Sở thích onboarding (UserPreferences) map sang danh mục Marketplace
/// 2. Sản phẩm từ các Shop mà người dùng đang Theo dõi (Follows)
/// 3. Sản phẩm được gắn thẻ trong các bài viết mạng xã hội mà người dùng vừa Thích (PostProduct)
/// </summary>
public class SocialAffinityCandidateGenerator
{
    private readonly SocialDbContext _context;
    private readonly ILogger<SocialAffinityCandidateGenerator> _logger;

    public SocialAffinityCandidateGenerator(
        SocialDbContext context,
        ILogger<SocialAffinityCandidateGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketplaceCandidateItem>> GenerateCandidatesAsync(
        Guid? userId,
        int quota = 60,
        CancellationToken cancellationToken = default)
    {
        // Khách vãng lai chưa đăng nhập -> Không có dữ liệu mạng xã hội
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Array.Empty<MarketplaceCandidateItem>();
        }

        var candidates = new List<MarketplaceCandidateItem>();
        var nowUtc = DateTime.UtcNow;

        try
        {
            // 1. Tín hiệu từ các Shop mà User đang Follow trên mạng xã hội
            var followedUserIds = await _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == userId.Value)
                .Select(f => f.FollowingId)
                .Take(50)
                .ToListAsync(cancellationToken);

            List<Guid> followedShopIds = new();
            if (followedUserIds.Count > 0)
            {
                followedShopIds = await _context.Shops
                    .AsNoTracking()
                    .Where(s => followedUserIds.Contains(s.OwnerUserId) &&
                                s.DeletedAtUtc == null &&
                                s.ApprovalStatus == ApprovalStatus.Approved &&
                                s.OperatingStatus == OperatingStatus.Open)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);
            }

            if (followedShopIds.Count > 0)
            {
                var shopProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Shop)
                    .Include(p => p.Category)
                    .Include(p => p.Images.Where(i => i.IsCover))
                    .Where(p => followedShopIds.Contains(p.ShopId) &&
                                p.DeletedAtUtc == null &&
                                p.Status == ProductStatus.Listed &&
                                !p.Takedowns.Any(t => t.LiftedAtUtc == null))
                    .OrderByDescending(p => p.RatingAverage)
                    .ThenByDescending(p => p.CreatedAtUtc)
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
                        BaseScore = 1.3,
                        Strategy = "SocialAffinity",
                        Reason = "Từ Shop bạn đang theo dõi"
                    })
                    .ToListAsync(cancellationToken);

                candidates.AddRange(shopProducts);
            }

            // 2. Tín hiệu từ Sở thích người dùng (UserPreferences) map sang Category
            var userPreferences = await _context.UserPreferences
                .AsNoTracking()
                .Include(up => up.Interest)
                .Where(up => up.UserId == userId.Value && up.Score > 0)
                .OrderByDescending(up => up.Score)
                .Take(5)
                .ToListAsync(cancellationToken);

            if (userPreferences.Count > 0)
            {
                var interestKeywords = userPreferences
                    .Select(up => up.Interest.Name.Trim().ToLower())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                // Tìm danh mục có tên tương đồng với sở thích
                var matchedCategoryIds = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && interestKeywords.Any(k => c.Name.ToLower().Contains(k)))
                    .Select(c => c.Id)
                    .Take(10)
                    .ToListAsync(cancellationToken);

                if (matchedCategoryIds.Count > 0)
                {
                    var interestProducts = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Shop)
                        .Include(p => p.Category)
                        .Include(p => p.Images.Where(i => i.IsCover))
                        .Where(p => p.CategoryId.HasValue && matchedCategoryIds.Contains(p.CategoryId.Value) &&
                                    p.DeletedAtUtc == null &&
                                    p.Status == ProductStatus.Listed &&
                                    !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                                    p.Shop != null && p.Shop.DeletedAtUtc == null &&
                                    p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                                    p.Shop.OperatingStatus == OperatingStatus.Open &&
                                    !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
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
                            BaseScore = 1.15,
                            Strategy = "SocialAffinity",
                            Reason = "Dựa trên sở thích của bạn"
                        })
                        .ToListAsync(cancellationToken);

                    candidates.AddRange(interestProducts);
                }
            }

            // 3. Tín hiệu từ Sản phẩm được gắn thẻ trong Bài viết User vừa Like (PostProduct)
            var likedPostIds = await _context.PostLikes
                .AsNoTracking()
                .Where(pl => pl.UserId == userId.Value)
                .OrderByDescending(pl => pl.CreatedAtUtc)
                .Take(20)
                .Select(pl => pl.PostId)
                .ToListAsync(cancellationToken);

            if (likedPostIds.Count > 0)
            {
                var taggedProductIds = await _context.PostProducts
                    .AsNoTracking()
                    .Where(pp => likedPostIds.Contains(pp.PostId))
                    .Select(pp => pp.ProductId)
                    .Distinct()
                    .Take(15)
                    .ToListAsync(cancellationToken);

                if (taggedProductIds.Count > 0)
                {
                    var taggedProducts = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Shop)
                        .Include(p => p.Category)
                        .Include(p => p.Images.Where(i => i.IsCover))
                        .Where(p => taggedProductIds.Contains(p.Id) &&
                                    p.DeletedAtUtc == null &&
                                    p.Status == ProductStatus.Listed &&
                                    !p.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                                    p.Shop != null && p.Shop.DeletedAtUtc == null &&
                                    p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                                    p.Shop.OperatingStatus == OperatingStatus.Open &&
                                    !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
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
                            BaseScore = 1.4,
                            Strategy = "SocialAffinity",
                            Reason = "Gắn thẻ trong bài viết bạn quan tâm"
                        })
                        .ToListAsync(cancellationToken);

                    candidates.AddRange(taggedProducts);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong SocialAffinityCandidateGenerator cho User {UserId}.", userId);
        }

        return candidates;
    }
}
