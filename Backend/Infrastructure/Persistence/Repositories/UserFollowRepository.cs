using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserFollowRepository : IUserFollowRepository
{
    private readonly SocialDbContext _context;

    public UserFollowRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AnyAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId, cancellationToken);
    }

    public async Task<UserFollow?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId, cancellationToken);
    }

    public async Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default)
    {
        await _context.UserFollows.AddAsync(follow, cancellationToken);
    }

    public void Delete(UserFollow follow)
    {
        _context.UserFollows.Remove(follow);
    }

    public async Task<int> GetFollowersCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .CountAsync(uf => uf.FollowingId == userId, cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .CountAsync(uf => uf.FollowerId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserFollow>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .Include(uf => uf.Follower)
            .Where(uf => uf.FollowingId == userId)
            .OrderByDescending(uf => uf.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserFollow>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .Include(uf => uf.Following)
            .Where(uf => uf.FollowerId == userId)
            .OrderByDescending(uf => uf.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetFollowingUserIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .Where(uf => uf.FollowerId == userId)
            .Select(uf => uf.FollowingId)
            .ToListAsync(cancellationToken);
    }
}
