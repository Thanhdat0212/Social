using Infrastructure.Persistence;
using Application.Interfaces.Repositories.Marketplace.Shops;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Marketplace.Shops;

public class ShopRepository : IShopRepository
{
    private readonly SocialDbContext _context;

    public ShopRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .Include(s => s.OwnerUser)
            .Include(s => s.Reviews)
            .Include(s => s.Sanctions)
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .Include(s => s.OwnerUser)
            .Include(s => s.Sanctions)
            .FirstOrDefaultAsync(s => s.Slug == slug.ToLower() && s.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<Shop?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .Include(s => s.OwnerUser)
            .Include(s => s.Reviews)
            .Include(s => s.Sanctions)
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId && s.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Shops.AnyAsync(s => s.Slug == slug.ToLower() && s.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<bool> ExistsByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Shops.AnyAsync(s => s.OwnerUserId == ownerUserId && s.DeletedAtUtc == null, cancellationToken);
    }

    public async Task AddAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        await _context.Shops.AddAsync(shop, cancellationToken);
    }

    public void Update(Shop shop)
    {
        _context.Shops.Update(shop);
    }

    public async Task<IReadOnlyList<Shop>> GetShopsAsync(
        ApprovalStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shops
            .Include(s => s.OwnerUser)
            .Include(s => s.Sanctions)
            .Where(s => s.DeletedAtUtc == null)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.ApprovalStatus == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(searchLower) || s.Slug.ToLower().Contains(searchLower));
        }

        return await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        ApprovalStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shops
            .Where(s => s.DeletedAtUtc == null)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.ApprovalStatus == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(searchLower) || s.Slug.ToLower().Contains(searchLower));
        }

        return await query.CountAsync(cancellationToken);
    }
}

public class ShopReviewRepository : IShopReviewRepository
{
    private readonly SocialDbContext _context;

    public ShopReviewRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShopReview>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopReviews
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.SubmissionNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopReview?> GetLatestByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopReviews
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.SubmissionNo)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ShopReview review, CancellationToken cancellationToken = default)
    {
        await _context.ShopReviews.AddAsync(review, cancellationToken);
    }

    public async Task<int> GetSubmissionCountAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopReviews
            .CountAsync(r => r.ShopId == shopId, cancellationToken);
    }
}

public class ShopSanctionRepository : IShopSanctionRepository
{
    private readonly SocialDbContext _context;

    public ShopSanctionRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShopSanction>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopSanctions
            .Where(s => s.ShopId == shopId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopSanction?> GetActiveSanctionByShopIdAsync(Guid shopId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.ShopSanctions
            .Where(s => s.ShopId == shopId && s.LiftedAtUtc == null && (s.EndAtUtc == null || s.EndAtUtc > nowUtc))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShopSanction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShopSanctions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(ShopSanction sanction, CancellationToken cancellationToken = default)
    {
        await _context.ShopSanctions.AddAsync(sanction, cancellationToken);
    }
}
