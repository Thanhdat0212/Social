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

    public async Task<IReadOnlyList<PostDto>> GetForYouFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default)
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

        // 1. Tập hợp danh sách ID các bài viết đã xem (kết hợp từ DB và Client gửi lên)
        var viewedPostIds = new HashSet<Guid>();
        if (seenPostIds != null)
        {
            foreach (var id in seenPostIds)
            {
                viewedPostIds.Add(id);
            }
        }

        if (currentUserId.HasValue)
        {
            var dbViewed = await _unitOfWork.UserInteractions.GetViewedPostIdsAsync(currentUserId.Value, 500, cancellationToken);
            foreach (var id in dbViewed)
            {
                viewedPostIds.Add(id);
            }
        }

        // 2. Phân chia ứng viên thành 2 nhóm: Chưa xem (Unseen) và Đã xem (Viewed)
        var unseenCandidates = candidatePosts.Where(p => !viewedPostIds.Contains(p.Id)).ToList();
        var viewedCandidates = candidatePosts.Where(p => viewedPostIds.Contains(p.Id)).ToList();

        List<Post> rankedPosts;

        if (currentUserId.HasValue)
        {
            // Lấy hồ sơ sở thích của người dùng để xếp hạng theo AI
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            var prefMap = preferences.ToDictionary(p => p.InterestId, p => p.Score);

            List<Post> RankCandidateGroup(List<Post> candidates)
            {
                if (candidates.Count == 0) return new List<Post>();

                var scoredList = new List<(Post Post, double TotalScore)>();
                foreach (var post in candidates)
                {
                    // A. Điểm khớp chủ đề với AI Confidence
                    double personalScore = 0.5;
                    if (post.PostInterests.Count > 0)
                    {
                        personalScore = 0.0;
                        foreach (var pi in post.PostInterests)
                        {
                            double userTopicScore = prefMap.TryGetValue(pi.InterestId, out var score) ? score : 0.5;
                            personalScore += userTopicScore * pi.Confidence;
                        }
                    }

                    // B. Điểm tương tác cộng đồng (Engagement)
                    double rawEngagement = (post.LikeCount * 2.0) + (post.CommentCount * 3.0) + (post.ViewCount * 0.2);
                    double engagementScore = Math.Log(1.0 + rawEngagement);

                    // C. Độ mới của bài viết (Time Decay)
                    double hoursOld = Math.Max(0.0, (now - post.CreatedAtUtc).TotalHours);
                    double recencyScore = 1.0 / (1.0 + 0.03 * hoursOld);

                    double totalScore = (personalScore * 0.55) + (engagementScore * 0.25) + (recencyScore * 0.20);
                    scoredList.Add((post, totalScore));
                }

                // Phân bổ đa dạng hóa theo tỷ lệ: 70% Cá nhân hóa - 20% Mở rộng - 10% Khám phá mới
                var orderedByScore = scoredList.OrderByDescending(x => x.TotalScore).ToList();

                var personalPool = orderedByScore
                    .Take((int)Math.Ceiling(candidates.Count * 0.7))
                    .Select(x => x.Post)
                    .ToList();

                var explorationPool = orderedByScore
                    .Skip((int)Math.Ceiling(candidates.Count * 0.7))
                    .Take((int)Math.Ceiling(candidates.Count * 0.2))
                    .Select(x => x.Post)
                    .ToList();

                var discoveryPool = candidates
                    .Except(personalPool)
                    .Except(explorationPool)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .ToList();

                return InterleaveFeed(personalPool, explorationPool, discoveryPool);
            }

            var rankedUnseen = RankCandidateGroup(unseenCandidates);
            var rankedViewed = RankCandidateGroup(viewedCandidates);

            // BÀI ĐĂNG CHƯA XEM ĐƯỢC ƯU TIÊN LÊN ĐẦU TIÊN
            // Nếu đã xem hết bài mới, các bài đã xem trước đó mới hiển thị ở phần sau
            rankedPosts = rankedUnseen.Concat(rankedViewed).ToList();
        }
        else
        {
            // Đối với khách vãng lai: Xếp hạng bài chưa xem trước, sau đó là bài đã xem
            List<Post> RankGuestGroup(List<Post> candidates)
            {
                return candidates
                    .OrderByDescending(p => (p.LikeCount * 2.0 + p.CommentCount * 3.0 + p.ViewCount * 0.2) / (1.0 + 0.05 * Math.Max(0, (now - p.CreatedAtUtc).TotalHours)))
                    .ThenByDescending(p => p.CreatedAtUtc)
                    .ToList();
            }

            var rankedUnseen = RankGuestGroup(unseenCandidates);
            var rankedViewed = RankGuestGroup(viewedCandidates);

            rankedPosts = rankedUnseen.Concat(rankedViewed).ToList();
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
