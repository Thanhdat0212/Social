using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<Post?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetRecentPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetCandidatePostsForFeedAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetFollowingPostsAsync(IEnumerable<Guid> followingUserIds, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var idList = followingUserIds.ToList();
        if (idList.Count == 0) return Array.Empty<Post>();

        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Where(p => idList.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
