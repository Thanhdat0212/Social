using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IInterestRepository : IGenericRepository<Interest>
{
    Task<IReadOnlyList<Interest>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Interest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Interest?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
