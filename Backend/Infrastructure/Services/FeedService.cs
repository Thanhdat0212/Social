using Application.Common;
using Application.DTOs.Posts;
using Application.Interfaces;
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
    private readonly IMapper _mapper;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<FeedService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PostDto>> GetForYouFeedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        // Lấy danh sách bài viết ứng viên gần nhất từ Database (Candidate Pool)
        var candidatePosts = await _unitOfWork.Posts.GetCandidatePostsForFeedAsync(200, cancellationToken);
        if (candidatePosts.Count == 0)
        {
            return Array.Empty<PostDto>();
        }

        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : null;
        List<Post> rankedPosts;

        if (currentUserId.HasValue)
        {
            // 1. Lấy hồ sơ sở thích của người dùng
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            var prefMap = preferences.ToDictionary(p => p.InterestId, p => p.Score);

            // 2. Lấy các bài viết người dùng đã xem gần đây để giảm ưu tiên (tránh lặp lại)
            var recentInteractions = await _unitOfWork.UserInteractions.GetRecentUserInteractionsAsync(
                currentUserId.Value, 200, cancellationToken);

            var viewedPostIds = recentInteractions
                .Where(i => i.InteractionType == InteractionType.View && i.PostId.HasValue)
                .Select(i => i.PostId!.Value)
                .ToHashSet();

            // 3. Tính điểm gợi ý (Scoring Model: Personal Match 55% + Engagement 25% + Recency Decay 20%)
            var scoredList = new List<(Post Post, double TotalScore, double PersonalScore)>();

            foreach (var post in candidatePosts)
            {
                // A. Điểm khớp chủ đề với AI Confidence
                double personalScore = 0.0;
                if (post.PostInterests.Count > 0)
                {
                    foreach (var pi in post.PostInterests)
                    {
                        double userTopicScore = prefMap.TryGetValue(pi.InterestId, out var score) ? score : 0.5;
                        personalScore += userTopicScore * pi.Confidence;
                    }
                }
                else
                {
                    personalScore = 0.5;
                }

                // B. Điểm tương tác cộng đồng (Engagement)
                double rawEngagement = (post.LikeCount * 2.0) + (post.CommentCount * 3.0) + (post.ViewCount * 0.2);
                double engagementScore = Math.Log(1.0 + rawEngagement);

                // C. Độ mới của bài viết (Time Decay: suy giảm dần theo số giờ)
                double hoursOld = Math.Max(0.0, (now - post.CreatedAtUtc).TotalHours);
                double recencyScore = 1.0 / (1.0 + 0.03 * hoursOld);

                // D. Hệ số bài đã xem (giảm 70% điểm nếu đã xem trong phiên gần đây)
                double viewedPenalty = viewedPostIds.Contains(post.Id) ? 0.3 : 1.0;

                double totalScore = ((personalScore * 0.55) + (engagementScore * 0.25) + (recencyScore * 0.20)) * viewedPenalty;

                scoredList.Add((post, totalScore, personalScore));
            }

            // 4. Phân bổ đa dạng hóa theo tỷ lệ (Feed Diversification: 70% Cá nhân hóa - 20% Mở rộng - 10% Khám phá mới)
            var orderedByScore = scoredList.OrderByDescending(x => x.TotalScore).ToList();

            // Nhóm 1: 70% Bài viết đúng sở thích cao nhất
            var personalPool = orderedByScore
                .Take((int)Math.Ceiling(candidatePosts.Count * 0.7))
                .Select(x => x.Post)
                .ToList();

            // Nhóm 2: 20% Bài viết mở rộng sở thích
            var explorationPool = orderedByScore
                .Skip((int)Math.Ceiling(candidatePosts.Count * 0.7))
                .Take((int)Math.Ceiling(candidatePosts.Count * 0.2))
                .Select(x => x.Post)
                .ToList();

            // Nhóm 3: 10% Bài viết mới / khám phá
            var discoveryPool = candidatePosts
                .Except(personalPool)
                .Except(explorationPool)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToList();

            // Trộn các nhóm một cách hài hoà
            rankedPosts = InterleaveFeed(personalPool, explorationPool, discoveryPool);
        }
        else
        {
            // Đối với khách vãng lai: Xếp theo tương tác và độ mới
            rankedPosts = candidatePosts
                .OrderByDescending(p => (p.LikeCount * 2.0 + p.CommentCount * 3.0 + p.ViewCount * 0.2) / (1.0 + 0.05 * Math.Max(0, (now - p.CreatedAtUtc).TotalHours)))
                .ThenByDescending(p => p.CreatedAtUtc)
                .ToList();
        }

        // Phân trang kết quả
        var pagedPosts = rankedPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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

    public async Task<IReadOnlyList<PostDto>> GetFollowingFeedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
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

        var posts = await _unitOfWork.Posts.GetFollowingPostsAsync(followingUserIds, page, pageSize, cancellationToken);
        var dtos = _mapper.Map<List<PostDto>>(posts);

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
    /// Phối trộn 3 danh sách theo chu kỳ: 7 bài Cá nhân hóa -> 2 bài Mở rộng -> 1 bài Khám phá mới
    /// </summary>
    private static List<Post> InterleaveFeed(List<Post> personal, List<Post> exploration, List<Post> discovery)
    {
        var result = new List<Post>(personal.Count + exploration.Count + discovery.Count);
        int pIndex = 0, eIndex = 0, dIndex = 0;

        while (pIndex < personal.Count || eIndex < exploration.Count || dIndex < discovery.Count)
        {
            // 7 bài từ Personal
            for (int i = 0; i < 7 && pIndex < personal.Count; i++)
            {
                result.Add(personal[pIndex++]);
            }

            // 2 bài từ Exploration
            for (int i = 0; i < 2 && eIndex < exploration.Count; i++)
            {
                result.Add(exploration[eIndex++]);
            }

            // 1 bài từ Discovery
            if (dIndex < discovery.Count)
            {
                result.Add(discovery[dIndex++]);
            }
        }

        return result;
    }
}
