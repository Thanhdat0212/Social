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

    public async Task<IReadOnlyList<Post>> GetPostsByInterestIdsAsync(IEnumerable<Guid> interestIds, int limit = 300, CancellationToken cancellationToken = default)
    {
        var idList = interestIds.ToList();
        if (idList.Count == 0) return Array.Empty<Post>();

        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Where(p => p.PostInterests.Any(pi => idList.Contains(pi.InterestId)))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetTrendingPostsAsync(TimeSpan timeWindow, int limit = 100, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        // Ưu tiên bài viết trong khung thời gian có tương tác cao
        var recentTrending = await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Where(p => p.CreatedAtUtc >= cutoff)
            .OrderByDescending(p => (p.LikeCount * 2) + (p.CommentCount * 3) + (p.ViewCount * 0.2))
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (recentTrending.Count >= limit / 2)
        {
            return recentTrending;
        }

        // Nếu hệ thống ít bài trong 48h, bổ sung bài có tương tác cao tổng thể
        var existingIds = recentTrending.Select(p => p.Id).ToHashSet();
        var fallback = await _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Where(p => !existingIds.Contains(p.Id))
            .OrderByDescending(p => (p.LikeCount * 2) + (p.CommentCount * 3) + (p.ViewCount * 0.2))
            .Take(limit - recentTrending.Count)
            .ToListAsync(cancellationToken);

        recentTrending.AddRange(fallback);
        return recentTrending;
    }

    public async Task<IReadOnlyList<Post>> GetExplorationPostsAsync(IEnumerable<Guid> excludeInterestIds, int limit = 100, CancellationToken cancellationToken = default)
    {
        var excludeList = excludeInterestIds.ToList();

        var query = _dbSet
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .AsQueryable();

        if (excludeList.Count > 0)
        {
            // Lấy các bài có topic không thuộc danh sách đã quen thuộc hoặc chưa có topic
            query = query.Where(p => !p.PostInterests.Any(pi => excludeList.Contains(pi.InterestId)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

