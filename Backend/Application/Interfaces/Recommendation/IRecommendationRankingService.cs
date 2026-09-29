using Application.DTOs.Recommendation;

namespace Application.Interfaces.Recommendation;

public interface IRecommendationRankingService
{
    Task<IReadOnlyList<ScoredCandidateDto>> RankAsync(
        IReadOnlyList<CandidatePostDto> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
