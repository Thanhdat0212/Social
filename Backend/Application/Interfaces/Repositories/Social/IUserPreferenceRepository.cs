using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserPreferenceRepository
{
    Task<IReadOnlyList<UserPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InitializePreferencesAsync(Guid userId, IEnumerable<Guid> interestIds, double initialScore = 1.0, CancellationToken cancellationToken = default);
    Task UpsertPreferenceAsync(Guid userId, Guid interestId, double scoreDelta, CancellationToken cancellationToken = default);
}
