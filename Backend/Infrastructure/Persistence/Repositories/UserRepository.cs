using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }
}
