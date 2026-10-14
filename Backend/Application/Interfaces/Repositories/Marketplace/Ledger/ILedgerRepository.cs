using Domain.Entities.Marketplace.Ledger;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Ledger;

public interface ILedgerRepository
{
    Task<LedgerAccount?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<LedgerAccount> GetOrCreateSystemAccountAsync(LedgerAccountType type, CancellationToken cancellationToken = default);
    Task<LedgerAccount> GetOrCreateShopAccountAsync(Guid shopId, LedgerAccountType type, CancellationToken cancellationToken = default);

    Task<bool> TransactionExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default);

    Task<ShopBalanceCache?> GetBalanceCacheAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<ShopBalanceCache> GetOrCreateBalanceCacheAsync(Guid shopId, CancellationToken cancellationToken = default);
    void UpdateBalanceCache(ShopBalanceCache cache);

    Task<(List<LedgerEntry> Items, int TotalCount)> GetShopStatementAsync(
        Guid shopId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
