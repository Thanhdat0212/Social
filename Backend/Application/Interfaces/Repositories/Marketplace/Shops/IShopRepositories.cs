using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Shops;

public interface IShopRepository
{
    Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Shop?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Shop shop, CancellationToken cancellationToken = default);
    void Update(Shop shop);
    Task<IReadOnlyList<Shop>> GetShopsAsync(ApprovalStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(ApprovalStatus? status, string? search, CancellationToken cancellationToken = default);
}

public interface IShopReviewRepository
{
    Task<IReadOnlyList<ShopReview>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<ShopReview?> GetLatestByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(ShopReview review, CancellationToken cancellationToken = default);
    Task<int> GetSubmissionCountAsync(Guid shopId, CancellationToken cancellationToken = default);
}

public interface IShopSanctionRepository
{
    Task<IReadOnlyList<ShopSanction>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<ShopSanction?> GetActiveSanctionByShopIdAsync(Guid shopId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<ShopSanction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ShopSanction sanction, CancellationToken cancellationToken = default);
}
