using Application.DTOs.Marketplace.Shops;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Shops;

public interface IShopService
{
    Task<ShopDetailDto> CreateShopAsync(CreateShopRequestDto request, CancellationToken cancellationToken = default);
    Task<ShopDetailDto?> GetMyShopAsync(CancellationToken cancellationToken = default);
    Task<ShopDetailDto> UpdateMyShopAsync(UpdateShopRequestDto request, CancellationToken cancellationToken = default);
    Task<ShopDetailDto> ResubmitShopAsync(ResubmitShopRequestDto request, CancellationToken cancellationToken = default);
    Task<ShopDetailDto> OpenMyShopAsync(CancellationToken cancellationToken = default);
    Task<ShopDetailDto> CloseMyShopAsync(CancellationToken cancellationToken = default);
    Task<ShopSummaryDto> GetPublicShopBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IAdminShopService
{
    Task<(IReadOnlyList<ShopSummaryDto> Items, int Total)> GetShopsAsync(
        ApprovalStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ShopDetailDto> GetShopDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopDetailDto> ApproveShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopDetailDto> RejectShopAsync(Guid id, RejectShopRequestDto request, CancellationToken cancellationToken = default);
    Task<ShopSanctionDto> AddSanctionAsync(Guid id, CreateSanctionRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> LiftSanctionAsync(Guid id, Guid sanctionId, CancellationToken cancellationToken = default);
}
