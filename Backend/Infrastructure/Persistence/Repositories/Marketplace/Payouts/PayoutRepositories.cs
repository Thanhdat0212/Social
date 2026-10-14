using Application.Interfaces.Repositories.Marketplace.Payouts;
using Domain.Entities.Marketplace.Payouts;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Payouts;

public class ShopBankAccountRepository : IShopBankAccountRepository
{
    private readonly SocialDbContext _dbContext;

    public ShopBankAccountRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopBankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopBankAccounts
            .Include(b => b.Shop)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<ShopBankAccount?> GetActiveByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopBankAccounts
            .Where(b => b.ShopId == shopId && b.Status == BankAccountStatus.Verified)
            .OrderByDescending(b => b.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ShopBankAccount account, CancellationToken cancellationToken = default)
    {
        await _dbContext.ShopBankAccounts.AddAsync(account, cancellationToken);
    }

    public void Update(ShopBankAccount account)
    {
        _dbContext.ShopBankAccounts.Update(account);
    }
}

public class PayoutRepository : IPayoutRepository
{
    private readonly SocialDbContext _dbContext;

    private static readonly PayoutStatus[] ActiveStatuses =
    {
        PayoutStatus.Requested,
        PayoutStatus.Approved,
        PayoutStatus.Processing,
        PayoutStatus.Unknown
    };

    public PayoutRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payout?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .Include(p => p.Shop)
            .Include(p => p.ShopBankAccount)
            .Include(p => p.StatusHistories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payout?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .Include(p => p.Shop)
            .Include(p => p.ShopBankAccount)
            .FirstOrDefaultAsync(p => p.RequestIdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<bool> HasActivePayoutAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .AnyAsync(p => p.ShopId == shopId && ActiveStatuses.Contains(p.Status), cancellationToken);
    }

    public async Task AddAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payouts.AddAsync(payout, cancellationToken);
    }

    public void Update(Payout payout)
    {
        _dbContext.Payouts.Update(payout);
    }

    public async Task<(List<Payout> Items, int TotalCount)> GetShopPayoutsAsync(
        Guid shopId,
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Payouts
            .Include(p => p.ShopBankAccount)
            .Where(p => p.ShopId == shopId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<Payout> Items, int TotalCount)> GetAdminPayoutsAsync(
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Payouts
            .Include(p => p.Shop)
            .Include(p => p.ShopBankAccount)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
