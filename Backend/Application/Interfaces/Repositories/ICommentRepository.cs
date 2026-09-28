using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<IReadOnlyList<Comment>> GetByPostIdAsync(Guid postId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<int> GetCountByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<Comment?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
