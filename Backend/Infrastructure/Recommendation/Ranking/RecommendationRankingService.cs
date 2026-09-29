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
            double freshnessScore = 100.0 / (1.0 + 0.03 * hoursOld);

            // 6. PopularityScore (0 - 100): Logarithmic engagement
            double rawEngagement = (post.LikeCount * 2.0) + (post.CommentCount * 3.0) + (post.ViewCount * 0.2);
            double popularityScore = Math.Min(100.0, 15.0 * Math.Log(1.0 + rawEngagement));

            // Trọng số theo kế hoạch đề xuất:
            // 0.25 Interest + 0.25 Behavior + 0.20 Collaborative + 0.15 Semantic + 0.10 Freshness + 0.05 Popularity
            double finalScore =
                (0.25 * interestScore) +
                (0.25 * behaviorScore) +
                (0.20 * collaborativeScore) +
                (0.15 * semanticScore) +
                (0.10 * freshnessScore) +
                (0.05 * popularityScore);

            // Áp dụng trọng số bổ sung từ CandidateSource (vd: Following được ưu tiên nhẹ)
            finalScore *= candidate.SourceWeight;

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
