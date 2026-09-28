using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PostInterestRepository : IPostInterestRepository
{
    private readonly SocialDbContext _context;

    public PostInterestRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PostInterest>> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _context.PostInterests
            .Include(pi => pi.Interest)
            .Where(pi => pi.PostId == postId)
            .OrderByDescending(pi => pi.Confidence)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<PostInterest> postInterests, CancellationToken cancellationToken = default)
    {
        await _context.PostInterests.AddRangeAsync(postInterests, cancellationToken);
    }
}
