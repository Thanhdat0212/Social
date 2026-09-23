using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class VerificationTokenRepository : GenericRepository<VerificationToken>, IVerificationTokenRepository
{
    public VerificationTokenRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<VerificationToken?> GetValidTokenAsync(Guid userId, VerificationPurpose purpose, string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            vt => vt.UserId == userId 
               && vt.Purpose == purpose 
               && vt.TokenHash == tokenHash, 
            cancellationToken);
    }

    public async Task<VerificationToken?> GetLatestTokenAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vt => vt.UserId == userId && vt.Purpose == purpose)
            .OrderByDescending(vt => vt.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VerificationToken>> GetActiveTokensAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vt => vt.UserId == userId && vt.Purpose == purpose && vt.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);
    }
}
