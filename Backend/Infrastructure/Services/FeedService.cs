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

namespace Infrastructure.Services;

public class FeedService : IFeedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRecommendationService _recommendationService;
    private readonly IMapper _mapper;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRecommendationService recommendationService,
        IMapper mapper,
        ILogger<FeedService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _recommendationService = recommendationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PostDto>> GetForYouFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var currentUserId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : null;

        var recommendationContext = new RecommendationContext
        {
            UserId = currentUserId,
            Page = page,
            PageSize = pageSize,
            SeenPostIds = seenPostIds != null ? new HashSet<Guid>(seenPostIds) : new HashSet<Guid>(),
            TargetCandidateCount = 700
        };

        // Kích hoạt toàn bộ Pipeline gợi ý: Generators -> Aggregator -> Ranking -> Diversity
        var rankedPosts = await _recommendationService.GetForYouFeedPostsAsync(recommendationContext, cancellationToken);
        List<Post> pagedPosts;

        if (rankedPosts.Count == 0)
        {
            // Dự phòng an toàn: nếu pipeline gợi ý không trả về bài nào, lấy các bài viết ứng viên mới nhất
            var fallback = await _unitOfWork.Posts.GetCandidatePostsForFeedAsync(pageSize * 2, cancellationToken);
            pagedPosts = fallback
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            // Phân trang kết quả
            pagedPosts = rankedPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        if (pagedPosts.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var dtos = _mapper.Map<List<PostDto>>(pagedPosts);

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
        var dbViewed = await _unitOfWork.UserInteractions.GetViewedPostIdsAsync(currentUserId, 500, cancellationToken);
        foreach (var id in dbViewed) viewedPostIds.Add(id);

        var posts = await _unitOfWork.Posts.GetFollowingPostsAsync(followingUserIds, 1, 200, cancellationToken);
        var unseen = posts.Where(p => !viewedPostIds.Contains(p.Id)).OrderByDescending(p => p.CreatedAtUtc).ToList();
        var viewed = posts.Where(p => viewedPostIds.Contains(p.Id)).OrderByDescending(p => p.CreatedAtUtc).ToList();

        var rankedPosts = unseen.Concat(viewed)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
}

