using Domain.Entities.Marketplace.Payouts;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Payouts;

public interface IShopBankAccountRepository
{
    Task<ShopBankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopBankAccount?> GetActiveByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(ShopBankAccount account, CancellationToken cancellationToken = default);
    void Update(ShopBankAccount account);
}

public interface IPayoutRepository
{
    Task<Payout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payout?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payout?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> HasActivePayoutAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(Payout payout, CancellationToken cancellationToken = default);
    void Update(Payout payout);

    Task<(List<Payout> Items, int TotalCount)> GetShopPayoutsAsync(
        Guid shopId,
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<Payout> Items, int TotalCount)> GetAdminPayoutsAsync(
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
