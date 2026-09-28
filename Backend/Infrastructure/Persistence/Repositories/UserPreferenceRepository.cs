using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly SocialDbContext _context;

    public UserPreferenceRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPreferences
            .Include(up => up.Interest)
            .Where(up => up.UserId == userId)
            .OrderByDescending(up => up.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task InitializePreferencesAsync(Guid userId, IEnumerable<Guid> interestIds, double initialScore = 1.0, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserPreferences
            .Where(up => up.UserId == userId)
            .ToDictionaryAsync(up => up.InterestId, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var interestId in interestIds.Distinct())
        {
            if (existing.TryGetValue(interestId, out var pref))
            {
                pref.Score = Math.Max(pref.Score, initialScore);
                pref.UpdatedAtUtc = now;
            }
            else
            {
                await _context.UserPreferences.AddAsync(new UserPreference
                {
                    UserId = userId,
                    InterestId = interestId,
                    Score = initialScore,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }, cancellationToken);
            }
        }
    }

    public async Task UpsertPreferenceAsync(Guid userId, Guid interestId, double scoreDelta, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pref = await _context.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == userId && up.InterestId == interestId, cancellationToken);

        if (pref != null)
        {
            pref.Score = Math.Max(0.0, pref.Score + scoreDelta);
            pref.UpdatedAtUtc = now;
        }
        else
        {
            await _context.UserPreferences.AddAsync(new UserPreference
            {
                UserId = userId,
                InterestId = interestId,
                Score = Math.Max(0.0, 1.0 + scoreDelta),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, cancellationToken);
        }
    }
}
