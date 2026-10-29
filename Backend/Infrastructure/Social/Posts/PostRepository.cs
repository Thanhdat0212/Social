using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Social.Posts;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(SocialDbContext context) : base(context)
    {
    }

    private IQueryable<PostCandidateDto> GetCandidateProjectionQuery()
    {
        return _dbSet
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Select(p => new PostCandidateDto
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                CreatedAtUtc = p.CreatedAtUtc,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                ViewCount = p.ViewCount,
                PostInterests = p.PostInterests.Select(pi => new CandidatePostInterestDto
                {
                    InterestId = pi.InterestId,
                    Name = pi.Interest.Name,
                    Confidence = pi.Confidence
                }).ToList()
            });
    }

    private IQueryable<Post> GetBasePostQuery(bool asNoTracking = true)
    {
        var query = _dbSet.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .AsSplitQuery()
            .Include(p => p.Author)
            .Include(p => p.PostInterests)
                .ThenInclude(pi => pi.Interest)
            .Include(p => p.PostProducts.Where(pp => pp.Product.DeletedAtUtc == null && pp.Product.Status == ProductStatus.Listed))
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(pr => pr.Shop)
            .Include(p => p.PostProducts.Where(pp => pp.Product.DeletedAtUtc == null && pp.Product.Status == ProductStatus.Listed))
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(pr => pr.Images);
    }

    public async Task<Post?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetBasePostQuery(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetRecentPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await GetBasePostQuery()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetCandidatePostsForFeedAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        return await GetBasePostQuery()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetFollowingPostsAsync(IEnumerable<Guid> followingUserIds, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var idList = followingUserIds.ToList();
        if (idList.Count == 0) return Array.Empty<Post>();

        return await GetBasePostQuery()
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

        return await GetBasePostQuery()
            .Where(p => p.PostInterests.Any(pi => idList.Contains(pi.InterestId)))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetTrendingPostsAsync(TimeSpan timeWindow, int limit = 100, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await GetBasePostQuery()
            .OrderByDescending(p => p.CreatedAtUtc >= cutoff)
            .ThenByDescending(p => (p.LikeCount * 2) + (p.CommentCount * 3) + (p.ViewCount * 0.2))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetExplorationPostsAsync(IEnumerable<Guid> excludeInterestIds, int limit = 100, CancellationToken cancellationToken = default)
    {
        var excludeList = excludeInterestIds.ToList();
        var query = GetBasePostQuery();

        if (excludeList.Count > 0)
        {
            query = query.Where(p => !p.PostInterests.Any(pi => excludeList.Contains(pi.InterestId)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetPostsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<Post>();

        var posts = await GetBasePostQuery()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Bảo toàn đúng thứ tự danh sách ID đầu vào
        var postMap = posts.ToDictionary(p => p.Id);
        return idList.Where(id => postMap.ContainsKey(id)).Select(id => postMap[id]).ToList();
    }

    public async Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(Guid authorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await GetBasePostQuery()
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPostCountByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(p => p.AuthorId == authorId, cancellationToken);
    }

    // -------------------------------------------------------------
    // Candidate Retrieval DTO Projections (1 câu SQL duy nhất, siêu nhẹ)
    // -------------------------------------------------------------
    public async Task<IReadOnlyList<PostCandidateDto>> GetRecentCandidatesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await GetCandidateProjectionQuery()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetFollowingCandidatesAsync(IEnumerable<Guid> followingUserIds, int limit = 100, CancellationToken cancellationToken = default)
    {
        var idList = followingUserIds.ToList();
        if (idList.Count == 0) return Array.Empty<PostCandidateDto>();

        return await GetCandidateProjectionQuery()
            .Where(p => idList.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetInterestCandidatesAsync(IEnumerable<Guid> interestIds, int limit = 300, CancellationToken cancellationToken = default)
    {
        var idList = interestIds.ToList();
        if (idList.Count == 0) return Array.Empty<PostCandidateDto>();

        return await GetCandidateProjectionQuery()
            .Where(p => p.PostInterests.Any(pi => idList.Contains(pi.InterestId)))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetTrendingCandidatesAsync(TimeSpan timeWindow, int limit = 100, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        // Ưu tiên bài viết trong 48h lên đầu (true > false), sau đó xét điểm tương tác
        // Tự động bù bài cũ hơn có tương tác cao nếu hệ thống mới có ít bài trong 48h, 1 query duy nhất!
        return await GetCandidateProjectionQuery()
            .OrderByDescending(p => p.CreatedAtUtc >= cutoff)
            .ThenByDescending(p => (p.LikeCount * 2) + (p.CommentCount * 3) + (p.ViewCount * 0.2))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetExplorationCandidatesAsync(IEnumerable<Guid> excludeInterestIds, int limit = 100, CancellationToken cancellationToken = default)
    {
        var excludeList = excludeInterestIds.ToList();
        var query = GetCandidateProjectionQuery();

        if (excludeList.Count > 0)
        {
            query = query.Where(p => !p.PostInterests.Any(pi => excludeList.Contains(pi.InterestId)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetAuthorRecentCandidatesAsync(Guid authorId, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await GetCandidateProjectionQuery()
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostCandidateDto>> GetCandidatesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<PostCandidateDto>();

        var candidates = await GetCandidateProjectionQuery()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var map = candidates.ToDictionary(c => c.Id);
        return idList.Where(id => map.ContainsKey(id)).Select(id => map[id]).ToList();
    }
}
