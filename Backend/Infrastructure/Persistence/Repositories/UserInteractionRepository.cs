using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserInteractionRepository : GenericRepository<UserInteraction>, IUserInteractionRepository
{
    public UserInteractionRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UserInteraction>> GetRecentUserInteractionsAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.UserInteractions
            .Include(ui => ui.Post)
                .ThenInclude(p => p!.PostInterests)
            .Where(ui => ui.UserId == userId)
            .OrderByDescending(ui => ui.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetViewedPostIdsAsync(Guid userId, int limit = 500, CancellationToken cancellationToken = default)
    {
        var rawIds = await _context.UserInteractions
            .Where(ui => ui.UserId == userId && ui.InteractionType == InteractionType.View && ui.PostId.HasValue)
            .OrderByDescending(ui => ui.CreatedAtUtc)
            .Take(limit)
            .Select(ui => ui.PostId!.Value)
            .ToListAsync(cancellationToken);

        return rawIds.Distinct().ToList();
    }

    public async Task<bool> HasInteractedRecentlyAsync(
        Guid userId,
        Guid postId,
        InteractionType type,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await _context.UserInteractions
            .AnyAsync(ui => ui.UserId == userId 
                            && ui.PostId == postId 
                            && ui.InteractionType == type 
                            && ui.CreatedAtUtc >= cutoff, 
                cancellationToken);
    }
}
