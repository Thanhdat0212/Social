using Application.Common;
using Application.DTOs.Posts;
using Application.DTOs.Recommendation;
using Application.Interfaces;
using Application.Interfaces.Recommendation;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Social.Posts;

public sealed class FeedService : IFeedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRecommendationService _recommendationService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRecommendationService recommendationService,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<FeedService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _recommendationService = recommendationService;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PostDto>> GetForYouFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var currentUserId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : null;
        var cacheKey = $"feed:foryou:{currentUserId?.ToString() ?? "anon"}";

        var seenSet = seenPostIds != null ? new HashSet<Guid>(seenPostIds) : new HashSet<Guid>();

        // 1. Kiểm tra Cache tầng 1: Danh sách ID bài viết đã xếp hạng (Ranked Post IDs)
        // Áp dụng cho CẢ page 1 và phân trang, phục vụ tức thì (< 80ms)
        var cachedIds = await _cacheService.GetAsync<List<Guid>>(cacheKey, cancellationToken);
        if (cachedIds != null && cachedIds.Count > 0)
        {
            var pagedIds = SelectCandidateIdsWithRecirculation(cachedIds, seenSet, page, pageSize);
            if (pagedIds.Count > 0)
            {
                var cachedPosts = await _unitOfWork.Posts.GetPostsByIdsAsync(pagedIds, cancellationToken);
                if (cachedPosts.Count > 0)
                {
                    var postDict = cachedPosts.ToDictionary(p => p.Id);
                    var orderedPosts = pagedIds
                        .Where(id => postDict.ContainsKey(id))
                        .Select(id => postDict[id])
                        .ToList();

                    var cachedDtos = _mapper.Map<List<PostDto>>(orderedPosts);
                    if (currentUserId.HasValue && cachedDtos.Count > 0)
                    {
                        var likedIds = (await _unitOfWork.PostLikes.GetLikedPostIdsByUserAsync(
                            currentUserId.Value,
                            cachedDtos.Select(d => d.Id),
                            cancellationToken)).ToHashSet();

                        foreach (var dto in cachedDtos)
                        {
                            dto.IsLikedByCurrentUser = likedIds.Contains(dto.Id);
                        }
                    }
                    return cachedDtos;
                }
            }
        }

        var recommendationContext = new RecommendationContext
        {
            UserId = currentUserId,
            Page = page,
            PageSize = pageSize,
            SeenPostIds = seenSet,
            TargetCandidateCount = 80
        };

        // Nạp trước dữ liệu người dùng 1 lần duy nhất để tái sử dụng xuyên suốt các Generators và Aggregator
        if (currentUserId.HasValue)
        {
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            recommendationContext.UserPreferenceScores = preferences
                .Where(p => p.Score > 0)
                .ToDictionary(p => p.InterestId, p => p.Score);

            var staticInterests = await _unitOfWork.UserInterests.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            recommendationContext.UserInterestIds = staticInterests.Select(ui => ui.InterestId).ToList();

            recommendationContext.ViewedPostIds = await _unitOfWork.UserInteractions.GetViewedPostIdsAsync(currentUserId.Value, 500, cancellationToken);
            recommendationContext.FollowingUserIds = await _unitOfWork.UserFollows.GetFollowingUserIdsAsync(currentUserId.Value, cancellationToken);
        }

        // Kích hoạt toàn bộ Pipeline gợi ý: Generators (DTOs) -> Aggregator -> Ranking -> Diversity -> Ranked Post IDs
        var rankedPostIds = await _recommendationService.GetForYouFeedPostIdsAsync(recommendationContext, cancellationToken);
        List<Guid> finalPoolIds;

        if (rankedPostIds.Count == 0)
        {
            // Dự phòng an toàn: nếu pipeline gợi ý không trả về bài nào, lấy các bài viết mới nhất
            var fallback = await _unitOfWork.Posts.GetRecentCandidatesAsync(Math.Max(pageSize * 4, 40), cancellationToken);
            finalPoolIds = fallback.Select(p => p.Id).ToList();
        }
        else
        {
            finalPoolIds = rankedPostIds.ToList();
        }

        if (finalPoolIds.Count > 0)
        {
            // Lưu cache danh sách ID (5 phút cho user đã login, 10 phút cho anonymous vãng lai)
            var ttl = currentUserId.HasValue ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(10);
            await _cacheService.SetAsync(cacheKey, finalPoolIds, ttl, cancellationToken);
        }

        var targetPagedIds = SelectCandidateIdsWithRecirculation(finalPoolIds, seenSet, page, pageSize);

        if (targetPagedIds.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        // Hydration: Lấy bài viết đầy đủ chi tiết (kèm TaggedProducts, Author, Images) cho riêng số lượng của trang hiện tại (1 câu split query duy nhất)
        var detailedPosts = await _unitOfWork.Posts.GetPostsByIdsAsync(targetPagedIds, cancellationToken);
        if (detailedPosts.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var detailedDict = detailedPosts.ToDictionary(p => p.Id);
        var orderedDetailed = targetPagedIds
            .Where(id => detailedDict.ContainsKey(id))
            .Select(id => detailedDict[id])
            .ToList();

        var dtos = _mapper.Map<List<PostDto>>(orderedDetailed);

        // Gắn trạng thái Like của người dùng hiện tại
        if (currentUserId.HasValue && dtos.Count > 0)
        {
            var likedIds = (await _unitOfWork.PostLikes.GetLikedPostIdsByUserAsync(
                currentUserId.Value,
                dtos.Select(d => d.Id),
                cancellationToken)).ToHashSet();

            foreach (var dto in dtos)
            {
                dto.IsLikedByCurrentUser = likedIds.Contains(dto.Id);
            }
        }

        return dtos;
    }

    public async Task<IReadOnlyList<PostDto>> GetFollowingFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu đăng nhập để xem bảng tin người theo dõi.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var currentUserId = _currentUserService.UserId.Value;

        // Lấy danh sách ID những người mà user đang theo dõi
        var followingUserIds = await _unitOfWork.UserFollows.GetFollowingUserIdsAsync(currentUserId, cancellationToken);
        if (followingUserIds.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var viewedPostIds = new HashSet<Guid>();
        if (seenPostIds != null)
        {
            foreach (var id in seenPostIds) viewedPostIds.Add(id);
        }

        var fetchLimit = Math.Max(200, page * pageSize);
        var posts = await _unitOfWork.Posts.GetFollowingPostsAsync(followingUserIds, 1, fetchLimit, cancellationToken);
        if (posts.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var allFollowingIds = posts.Select(p => p.Id).ToList();
        var targetPagedIds = SelectCandidateIdsWithRecirculation(allFollowingIds, viewedPostIds, page, pageSize);

        var postDict = posts.ToDictionary(p => p.Id);
        var rankedPosts = targetPagedIds
            .Where(id => postDict.ContainsKey(id))
            .Select(id => postDict[id])
            .ToList();

        var dtos = _mapper.Map<List<PostDto>>(rankedPosts);

        if (dtos.Count > 0)
        {
            var likedIds = (await _unitOfWork.PostLikes.GetLikedPostIdsByUserAsync(
                currentUserId,
                dtos.Select(d => d.Id),
                cancellationToken)).ToHashSet();

            foreach (var dto in dtos)
            {
                dto.IsLikedByCurrentUser = likedIds.Contains(dto.Id);
            }
        }

        return dtos;
    }

    /// <summary>
    /// Thuật toán chọn bài viết hỗ trợ tuần hoàn vô hạn (Infinite Loop Recirculation)
    /// Ưu tiên bài chưa xem; khi hết bài mới, tự động xoay vòng bài cũ theo modulo để feed không bị dừng.
    /// </summary>
    private static List<Guid> SelectCandidateIdsWithRecirculation(
        List<Guid> pool,
        HashSet<Guid> seenSet,
        int page,
        int pageSize)
    {
        if (pool.Count == 0) return new List<Guid>();

        var selected = new List<Guid>();

        if (seenSet.Count > 0)
        {
            var unseen = pool.Where(id => !seenSet.Contains(id)).ToList();
            if (unseen.Count > 0)
            {
                selected.AddRange(unseen.Take(pageSize));
            }

            // Nếu đã cạn bài chưa xem hoặc không đủ pageSize, bù đắp bằng các bài cũ trong pool để tuần hoàn vô tận
            if (selected.Count < pageSize)
            {
                var alreadySelected = new HashSet<Guid>(selected);
                var remainingPool = pool.Where(id => !alreadySelected.Contains(id)).ToList();
                if (remainingPool.Count == 0)
                {
                    remainingPool = pool;
                }

                var offset = Math.Abs((page - 1) * pageSize) % remainingPool.Count;
                int needed = pageSize - selected.Count;
                for (int i = 0; i < needed && i < remainingPool.Count; i++)
                {
                    var candidateId = remainingPool[(offset + i) % remainingPool.Count];
                    if (!alreadySelected.Contains(candidateId))
                    {
                        selected.Add(candidateId);
                        alreadySelected.Add(candidateId);
                    }
                }
            }
        }
        else
        {
            // Không có seenSet, phân trang tuần hoàn theo modulo trên pool
            var offset = Math.Abs((page - 1) * pageSize) % pool.Count;
            for (int i = 0; i < pageSize && i < pool.Count; i++)
            {
                selected.Add(pool[(offset + i) % pool.Count]);
            }
        }

        return selected;
    }
}
