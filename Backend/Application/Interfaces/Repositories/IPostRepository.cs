using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetRecentPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetCandidatePostsForFeedAsync(int limit = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetFollowingPostsAsync(IEnumerable<Guid> followingUserIds, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
