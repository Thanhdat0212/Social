using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
