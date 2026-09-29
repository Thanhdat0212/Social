using Domain.Entities;

namespace Application.Interfaces.Recommendation;

public interface ISemanticVectorSearchService
{
    Task<float[]?> BuildUserInterestVectorAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Post>> FindSimilarPostsByVectorAsync(
        float[] userVector,
        IEnumerable<Guid>? excludePostIds = null,
        int topN = 200,
        CancellationToken cancellationToken = default);
}
