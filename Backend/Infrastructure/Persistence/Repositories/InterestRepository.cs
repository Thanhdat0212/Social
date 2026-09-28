using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class InterestRepository : GenericRepository<Interest>, IInterestRepository
{
    public InterestRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Interest>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Interest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _dbSet
            .Where(i => idList.Contains(i.Id) && i.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Interest?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.Slug == slug.ToLower(), cancellationToken);
    }
}
