using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PostLikeRepository : IPostLikeRepository
{
    private readonly SocialDbContext _context;

    public PostLikeRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsLikedAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        return await _context.PostLikes
            .AnyAsync(pl => pl.UserId == userId && pl.PostId == postId, cancellationToken);
    }

    public async Task<PostLike?> GetLikeAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        return await _context.PostLikes
            .FirstOrDefaultAsync(pl => pl.UserId == userId && pl.PostId == postId, cancellationToken);
    }

    public async Task AddAsync(PostLike like, CancellationToken cancellationToken = default)
    {
        await _context.PostLikes.AddAsync(like, cancellationToken);
    }

    public void Delete(PostLike like)
    {
        _context.PostLikes.Remove(like);
    }

    public async Task<int> GetCountByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _context.PostLikes
            .CountAsync(pl => pl.PostId == postId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetLikedPostIdsByUserAsync(Guid userId, IEnumerable<Guid> postIds, CancellationToken cancellationToken = default)
    {
        var postIdList = postIds.ToList();
        if (postIdList.Count == 0) return Array.Empty<Guid>();

        return await _context.PostLikes
            .Where(pl => pl.UserId == userId && postIdList.Contains(pl.PostId))
            .Select(pl => pl.PostId)
            .ToListAsync(cancellationToken);
    }
}
