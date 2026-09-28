using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserInterestRepository : IUserInterestRepository
{
    private readonly SocialDbContext _context;

    public UserInterestRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserInterest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserInterests
            .Include(ui => ui.Interest)
            .Where(ui => ui.UserId == userId)
            .OrderBy(ui => ui.Interest.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUserSelectedInterestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserInterests
            .AnyAsync(ui => ui.UserId == userId, cancellationToken);
    }

    public async Task SetUserInterestsAsync(Guid userId, IEnumerable<Guid> interestIds, CancellationToken cancellationToken = default)
    {
        // Xóa các sở thích cũ
        var existing = await _context.UserInterests
            .Where(ui => ui.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _context.UserInterests.RemoveRange(existing);
        }

        // Thêm các sở thích mới
        var newInterests = interestIds.Distinct().Select(id => new UserInterest
        {
            UserId = userId,
            InterestId = id,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.UserInterests.AddRangeAsync(newInterests, cancellationToken);
    }
}
