using Application.Interfaces.Repositories.Marketplace.Ledger;
using Domain.Entities.Marketplace.Ledger;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Ledger;

public class LedgerRepository : ILedgerRepository
{
    private readonly SocialDbContext _dbContext;

    public LedgerRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerAccount?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerAccounts
            .FirstOrDefaultAsync(a => a.Code == code.ToUpper(), cancellationToken);
    }

    public async Task<LedgerAccount> GetOrCreateSystemAccountAsync(LedgerAccountType type, CancellationToken cancellationToken = default)
    {
        var code = LedgerAccount.GenerateAccountCode(type, null);
        var account = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
        if (account != null) return account;

        var name = type switch
        {
            LedgerAccountType.GatewayClearing => "Tài khoản trung gian Cổng thanh toán",
            LedgerAccountType.UnallocatedPayments => "Tài khoản các khoản thu chờ phân bổ",
            LedgerAccountType.PlatformRevenue => "Doanh thu phí hoa hồng Sàn",
            LedgerAccountType.PlatformExpense => "Chi phí khuyến mãi / Hỗ trợ Sàn",
            LedgerAccountType.BankClearing => "Tài khoản đối ứng Ngân hàng giải ngân",
            LedgerAccountType.RefundPayable => "Khoản phải hoàn trả Người mua",
            _ => type.ToString()
        };

        account = new LedgerAccount(type, code, name, null);
        await _dbContext.LedgerAccounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<LedgerAccount> GetOrCreateShopAccountAsync(Guid shopId, LedgerAccountType type, CancellationToken cancellationToken = default)
    {
        var code = LedgerAccount.GenerateAccountCode(type, shopId);
        var account = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
        if (account != null) return account;

        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.Id == shopId, cancellationToken);
        var shopName = shop?.Name ?? shopId.ToString("N")[..8];

        var name = type switch
        {
            LedgerAccountType.ShopPending => $"Tiền tạm giữ (Pending) - {shopName}",
            LedgerAccountType.ShopAvailable => $"Tiền khả dụng (Available) - {shopName}",
            LedgerAccountType.PayoutPending => $"Tiền chờ rút (PayoutPending) - {shopName}",
            _ => $"{type} - {shopName}"
        };

        account = new LedgerAccount(type, code, name, shopId);
        await _dbContext.LedgerAccounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<bool> TransactionExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerTransactions
            .AnyAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddTransactionAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.LedgerTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<ShopBalanceCache?> GetBalanceCacheAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopBalanceCaches
            .FirstOrDefaultAsync(b => b.ShopId == shopId, cancellationToken);
    }

    public async Task<ShopBalanceCache> GetOrCreateBalanceCacheAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var cache = await _dbContext.ShopBalanceCaches.FirstOrDefaultAsync(b => b.ShopId == shopId, cancellationToken);
        if (cache != null) return cache;

        cache = new ShopBalanceCache(shopId);
        await _dbContext.ShopBalanceCaches.AddAsync(cache, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return cache;
    }

    public void UpdateBalanceCache(ShopBalanceCache cache)
    {
        _dbContext.ShopBalanceCaches.Update(cache);
    }

    public async Task<(List<LedgerEntry> Items, int TotalCount)> GetShopStatementAsync(
        Guid shopId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shopAccountIds = await _dbContext.LedgerAccounts
            .Where(a => a.ShopId == shopId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var query = _dbContext.LedgerEntries
            .Include(e => e.Transaction)
            .Include(e => e.Account)
            .Where(e => shopAccountIds.Contains(e.AccountId));

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.CreatedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.CreatedAtUtc <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
