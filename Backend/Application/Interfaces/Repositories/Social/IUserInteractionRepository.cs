using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IUserInteractionRepository : IGenericRepository<UserInteraction>
{
    Task<IReadOnlyList<UserInteraction>> GetRecentUserInteractionsAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetViewedPostIdsAsync(Guid userId, int limit = 500, CancellationToken cancellationToken = default);
    Task<bool> HasInteractedRecentlyAsync(Guid userId, Guid postId, InteractionType type, TimeSpan timeWindow, CancellationToken cancellationToken = default);
}
