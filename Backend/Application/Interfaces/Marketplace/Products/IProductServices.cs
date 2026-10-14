using Application.DTOs.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Products;

public interface IProductService
{
    Task<ProductDetailDto> CreateDraftAsync(Guid userId, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateDetailsAsync(Guid userId, Guid productId, UpdateProductDetailsRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateVariantsAsync(Guid userId, Guid productId, UpdateProductVariantsRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> SaveAttributesAsync(Guid userId, Guid productId, SaveProductAttributeValuesRequest request, CancellationToken cancellationToken = default);
    Task<ProductImageDto> AddImageAsync(Guid userId, Guid productId, UploadProductImageRequest request, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(Guid userId, Guid productId, Guid imageId, CancellationToken cancellationToken = default);
    Task ReorderImagesAsync(Guid userId, Guid productId, ReorderProductImagesRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> PublishAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> HideAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UnhideAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> ArchiveAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> GetShopProductsAsync(Guid userId, ProductStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductByIdAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}

public interface ICatalogService
{
    Task<PagedResult<CatalogProductCardDto>> GetCatalogProductsAsync(CatalogFilterQuery query, CancellationToken cancellationToken = default);
    Task<CatalogProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<PagedResult<CatalogProductCardDto>> GetShopPublicProductsAsync(string shopSlug, int page, int pageSize, string? search, CancellationToken cancellationToken = default);
}

public interface IAdminProductService
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(Guid? shopId, ProductStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task TakedownAsync(Guid adminId, Guid productId, string reason, CancellationToken cancellationToken = default);
    Task LiftTakedownAsync(Guid adminId, Guid productId, CancellationToken cancellationToken = default);
}
