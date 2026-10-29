using Application.DTOs.Recommendation;

namespace Application.Interfaces.Recommendation;

public interface IRecommendationService
{
    Task<IReadOnlyList<Guid>> GetForYouFeedPostIdsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
