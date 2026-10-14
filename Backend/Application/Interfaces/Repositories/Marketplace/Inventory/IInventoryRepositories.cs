using Domain.Entities.Marketplace.Inventory;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Inventory;

public record InventorySummary(int Total, int Available, int Reserved, int Delivered, int Invalid);

public interface IDigitalCodeRepository
{
    Task<DigitalCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(DigitalCode code, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DigitalCode> codes, CancellationToken cancellationToken = default);
    void Delete(DigitalCode code);

    Task<bool> ExistsHashInVariantAsync(Guid shopId, Guid variantId, string valueHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy tập hợp các hash đã tồn tại trong database để đối chiếu hàng loạt nhanh chóng (1 round-trip)
    /// </summary>
    Task<HashSet<string>> GetExistingHashesAsync(Guid shopId, Guid variantId, IEnumerable<string> hashes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DigitalCode>> GetCodesByVariantAsync(
        Guid shopId,
        Guid variantId,
        DigitalCodeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetCodesCountByVariantAsync(
        Guid shopId,
        Guid variantId,
        DigitalCodeStatus? status,
        CancellationToken cancellationToken = default);

    Task<InventorySummary> GetInventorySummaryAsync(Guid variantId, CancellationToken cancellationToken = default);
}

public interface IDigitalFileRepository
{
    Task<DigitalFile?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task AddAsync(DigitalFile file, CancellationToken cancellationToken = default);
    void Update(DigitalFile file);
    void Delete(DigitalFile file);
}
