using Application.DTOs.Recommendation;
using Domain.Entities;

namespace Application.Interfaces.Recommendation;

public interface IRecommendationService
{
    Task<IReadOnlyList<Post>> GetForYouFeedPostsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
