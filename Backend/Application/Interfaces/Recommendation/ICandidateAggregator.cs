using Application.DTOs.Recommendation;

namespace Application.Interfaces.Recommendation;

public interface ICandidateAggregator
{
    Task<IReadOnlyList<CandidatePostDto>> AggregateAsync(
        IReadOnlyList<CandidatePostDto> allCandidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
