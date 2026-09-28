using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(SocialDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Comment>> GetByPostIdAsync(Guid postId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .CountAsync(c => c.PostId == postId, cancellationToken);
    }

    public async Task<Comment?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
