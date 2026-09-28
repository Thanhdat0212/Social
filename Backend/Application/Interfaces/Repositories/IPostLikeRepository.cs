using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IPostLikeRepository
{
    Task<bool> IsLikedAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task<PostLike?> GetLikeAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task AddAsync(PostLike like, CancellationToken cancellationToken = default);
    void Delete(PostLike like);
    Task<int> GetCountByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetLikedPostIdsByUserAsync(Guid userId, IEnumerable<Guid> postIds, CancellationToken cancellationToken = default);
}
