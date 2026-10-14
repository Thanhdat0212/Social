using Application.Interfaces.Repositories.Marketplace.Inventory;
using Domain.Entities.Marketplace.Inventory;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Inventory;

public class DigitalCodeRepository : IDigitalCodeRepository
{
    private readonly SocialDbContext _context;

    public DigitalCodeRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<DigitalCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DigitalCodes
            .Include(c => c.Variant)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(DigitalCode code, CancellationToken cancellationToken = default)
    {
        await _context.DigitalCodes.AddAsync(code, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DigitalCode> codes, CancellationToken cancellationToken = default)
    {
        await _context.DigitalCodes.AddRangeAsync(codes, cancellationToken);
    }

    public void Delete(DigitalCode code)
    {
        _context.DigitalCodes.Remove(code);
    }

    public async Task<bool> ExistsHashInVariantAsync(Guid shopId, Guid variantId, string valueHash, CancellationToken cancellationToken = default)
    {
        return await _context.DigitalCodes
            .AnyAsync(c => c.ShopId == shopId && c.VariantId == variantId && c.ValueHash == valueHash, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingHashesAsync(Guid shopId, Guid variantId, IEnumerable<string> hashes, CancellationToken cancellationToken = default)
    {
        var hashList = hashes.ToList();
        var existing = await _context.DigitalCodes
            .Where(c => c.ShopId == shopId && c.VariantId == variantId && hashList.Contains(c.ValueHash))
            .Select(c => c.ValueHash)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }

    public async Task<IReadOnlyList<DigitalCode>> GetCodesByVariantAsync(
        Guid shopId,
        Guid variantId,
        DigitalCodeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalCodes
            .Where(c => c.ShopId == shopId && c.VariantId == variantId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCodesCountByVariantAsync(
        Guid shopId,
        Guid variantId,
        DigitalCodeStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DigitalCodes
            .Where(c => c.ShopId == shopId && c.VariantId == variantId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<InventorySummary> GetInventorySummaryAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.DigitalCodes
            .Where(c => c.VariantId == variantId)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int available = 0, reserved = 0, delivered = 0, invalid = 0;
        foreach (var item in counts)
        {
            switch (item.Status)
            {
                case DigitalCodeStatus.Available: available = item.Count; break;
                case DigitalCodeStatus.Reserved: reserved = item.Count; break;
                case DigitalCodeStatus.Delivered: delivered = item.Count; break;
                case DigitalCodeStatus.Invalid: invalid = item.Count; break;
            }
        }

        var total = available + reserved + delivered + invalid;
        return new InventorySummary(total, available, reserved, delivered, invalid);
    }
}

public class DigitalFileRepository : IDigitalFileRepository
{
    private readonly SocialDbContext _context;

    public DigitalFileRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<DigitalFile?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return await _context.DigitalFiles
            .FirstOrDefaultAsync(f => f.VariantId == variantId, cancellationToken);
    }

    public async Task AddAsync(DigitalFile file, CancellationToken cancellationToken = default)
    {
        await _context.DigitalFiles.AddAsync(file, cancellationToken);
    }

    public void Update(DigitalFile file)
    {
        _context.DigitalFiles.Update(file);
    }

    public void Delete(DigitalFile file)
    {
        _context.DigitalFiles.Remove(file);
    }
}
