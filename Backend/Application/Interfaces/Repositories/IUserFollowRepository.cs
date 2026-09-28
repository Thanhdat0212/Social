using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserFollowRepository
{
    Task<bool> IsFollowingAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken = default);
    Task<UserFollow?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken = default);
    Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default);
    void Delete(UserFollow follow);
    Task<int> GetFollowersCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowingCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserFollow>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserFollow>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetFollowingUserIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
