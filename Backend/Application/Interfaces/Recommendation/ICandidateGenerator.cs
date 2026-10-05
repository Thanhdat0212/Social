using Application.DTOs.Recommendation;
using Domain.Enums;

namespace Application.Interfaces.Recommendation;

public interface ICandidateGenerator
{
    CandidateSource Source { get; }
    int Priority { get; } // Ưu tiên 
    Task<IReadOnlyList<CandidatePostDto>> GenerateCandidatesAsync(
        RecommendationContext context,
        int targetCount,
        CancellationToken cancellationToken = default);
}
