using Domain.Entities.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    Task AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    void RemoveImage(ProductImage image);

    // Dành cho Seller quản lý sản phẩm trong Shop của mình
    Task<IReadOnlyList<Product>> GetShopProductsAsync(
        Guid shopId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetShopProductsCountAsync(
        Guid shopId,
        ProductStatus? status,
        string? search,
        CancellationToken cancellationToken = default);

    // Dành cho Khách mua hàng xem danh mục sản phẩm công khai
    Task<IReadOnlyList<Product>> GetPublicCatalogAsync(
        Guid? categoryId,
        string? search,
        long? minPrice,
        long? maxPrice,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetPublicCatalogCountAsync(
        Guid? categoryId,
        string? search,
        long? minPrice,
        long? maxPrice,
        CancellationToken cancellationToken = default);

    // Dành cho Khách mua hàng xem danh mục sản phẩm của một Shop cụ thể
    Task<IReadOnlyList<Product>> GetShopPublicProductsAsync(
        Guid shopId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetShopPublicProductsCountAsync(
        Guid shopId,
        string? search,
        CancellationToken cancellationToken = default);

    // Dành cho Quản trị viên Admin
    Task<IReadOnlyList<Product>> GetAdminProductsAsync(
        Guid? shopId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetAdminProductsCountAsync(
        Guid? shopId,
        ProductStatus? status,
        string? search,
        CancellationToken cancellationToken = default);
}

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySkuInShopAsync(Guid shopId, string skuNormalized, Guid? excludeVariantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    void Update(ProductVariant variant);
}
