using Domain.Entities;

namespace Application.Interfaces.Recommendation;

public interface ICollaborativeFilteringService
{
    Task<IReadOnlyList<(Guid UserId, double Similarity)>> FindSimilarUsersAsync(
        Guid userId,
        int topN = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Post>> GetCollaborativeCandidatesAsync(
        Guid userId,
        int targetCount = 200,
        CancellationToken cancellationToken = default);
}
