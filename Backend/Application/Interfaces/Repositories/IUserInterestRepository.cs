using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserInterestRepository
{
    Task<IReadOnlyList<UserInterest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasUserSelectedInterestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetUserInterestsAsync(Guid userId, IEnumerable<Guid> interestIds, CancellationToken cancellationToken = default);
}
