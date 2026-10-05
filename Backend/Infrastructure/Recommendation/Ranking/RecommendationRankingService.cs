using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.Ranking;

public class RecommendationRankingService : IRecommendationRankingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecommendationRankingService> _logger;

    public RecommendationRankingService(
        IUnitOfWork unitOfWork,
        ILogger<RecommendationRankingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScoredCandidateDto>> RankAsync(
        IReadOnlyList<CandidatePostDto> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return Array.Empty<ScoredCandidateDto>();
        }

        var now = DateTime.UtcNow;

        // Lấy thông tin sở thích nếu user đã đăng nhập
        Dictionary<Guid, double> prefMap = new();
        if (context.UserId.HasValue)
        {
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(context.UserId.Value, cancellationToken);
            prefMap = preferences.ToDictionary(p => p.InterestId, p => p.Score);
        }

        var scoredList = new List<ScoredCandidateDto>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var post = candidate.Post;

            // 1. InterestScore (0 - 100)
            double interestScore = 50.0;
            if (context.UserId.HasValue && post.PostInterests.Count > 0)
            {
                double totalMatch = 0.0;
                double totalConfidence = 0.0;
                foreach (var pi in post.PostInterests)
                {
                    double userScore = prefMap.TryGetValue(pi.InterestId, out var s) ? s : 0.5;
                    totalMatch += userScore * pi.Confidence;
                    totalConfidence += pi.Confidence;
                }

                var normalizedScore = totalConfidence > 0 ? (totalMatch / totalConfidence) : 0.5;
                // Chuẩn hóa sang thang điểm 100 (điểm sở thích thường dao động 0 - 5.0)
                interestScore = Math.Clamp(normalizedScore * 20.0, 0.0, 100.0);
            }

            // 2. BehaviorScore (0 - 100): Tạm thời dựa trên SourceWeight và tương tác
            double behaviorScore = Math.Clamp(candidate.SourceWeight * 50.0, 0.0, 100.0);

            // 3. CollaborativeScore (0 - 100)
            double collaborativeScore = candidate.Source == Domain.Enums.CandidateSource.Collaborative
                ? Math.Clamp(85.0 * candidate.SourceWeight, 0.0, 100.0)
                : Math.Clamp((interestScore * 0.7) + 15.0, 0.0, 100.0);

            // 4. SemanticScore (0 - 100)
            double semanticScore = candidate.Source == Domain.Enums.CandidateSource.Semantic
                ? Math.Clamp(85.0 * candidate.SourceWeight, 0.0, 100.0)
                : 50.0;

            // 5. FreshnessScore (0 - 100): Time decay
            double hoursOld = Math.Max(0.0, (now - post.CreatedAtUtc).TotalHours);
            // Hệ số suy giảm 0.08 giúp bài viết mới nổi trội hơn hẳn
            double freshnessScore = 100.0 / (1.0 + 0.08 * hoursOld);

            // 6. PopularityScore (0 - 100): Logarithmic engagement
            double rawEngagement = (post.LikeCount * 2.0) + (post.CommentCount * 3.0) + (post.ViewCount * 0.2);
            double popularityScore = Math.Min(100.0, 15.0 * Math.Log(1.0 + rawEngagement));

            // Trọng số đề xuất tối ưu: Ưu tiên mạnh tính thời sự (Freshness: 25%) để bài mới đăng đẩy lên feed ngay
            // 0.25 Freshness + 0.20 Interest + 0.20 Behavior + 0.15 Collaborative + 0.10 Semantic + 0.10 Popularity
            double finalScore =
                (0.20 * interestScore) +
                (0.20 * behaviorScore) +
                (0.15 * collaborativeScore) +
                (0.10 * semanticScore) +
                (0.25 * freshnessScore) +
                (0.10 * popularityScore);

            // Áp dụng trọng số từ CandidateSource (vd: Recent được ưu tiên 1.3x)
            finalScore *= candidate.SourceWeight;

            // 7. New Post Boost (Đặc quyền bài mới xuất bản để xuất hiện tức thì trên Feed)
            if (hoursOld < 0.5) // Dưới 30 phút
            {
                finalScore *= 1.60;
            }
            else if (hoursOld < 2.0) // Dưới 2 giờ
            {
                finalScore *= 1.35;
            }
            else if (hoursOld < 6.0) // Dưới 6 giờ
            {
                finalScore *= 1.15;
            }

            // 8. Author Boost: Bài viết do chính người dùng hiện tại đăng (< 24h) luôn được ưu tiên thấy trên feed của mình
            if (context.UserId.HasValue && post.AuthorId == context.UserId.Value && hoursOld < 24.0)
            {
                finalScore *= 1.50;
            }

            // 9. Xử lý bài đã xem (penalty) và bài chưa xem (exploration jitter)
            if (candidate.IsViewed)
            {
                // Giảm mạnh 85% điểm số đối với bài đã xem để bài chưa xem luôn đứng trên
                finalScore *= 0.15;
                var jitter = 0.85 + (Random.Shared.NextDouble() * 0.30);
                finalScore *= jitter;
            }
            else
            {
                // Thêm jitter nhẹ (±12%) cho bài chưa xem để mỗi lần reload/F5 không bị fix cứng một danh sách cố định
                var exploreJitter = 0.88 + (Random.Shared.NextDouble() * 0.24);
                finalScore *= exploreJitter;
            }

            scoredList.Add(new ScoredCandidateDto
            {
                Post = post,
                FinalScore = finalScore,
                InterestScore = interestScore,
                BehaviorScore = behaviorScore,
                CollaborativeScore = collaborativeScore,
                SemanticScore = semanticScore,
                FreshnessScore = freshnessScore,
                PopularityScore = popularityScore,
                PrimaryInterestId = candidate.PrimaryInterestId,
                PrimaryInterestName = candidate.PrimaryInterestName
            });
        }

        return scoredList.OrderByDescending(s => s.FinalScore).ToList();
    }
}
