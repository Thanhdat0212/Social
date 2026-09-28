using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IPostInterestRepository
{
    Task<IReadOnlyList<PostInterest>> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PostInterest> postInterests, CancellationToken cancellationToken = default);
}
