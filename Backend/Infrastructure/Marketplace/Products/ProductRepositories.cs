using Infrastructure.Persistence;
using Application.Interfaces.Repositories.Marketplace.Products;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Marketplace.Products;

public class ProductRepository : IProductRepository
{
    private readonly SocialDbContext _context;

    public ProductRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Shop)
                .ThenInclude(s => s!.OwnerUser)
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .Include(p => p.Options.OrderBy(o => o.Position))
                .ThenInclude(o => o.Values.OrderBy(v => v.Position))
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
                .ThenInclude(v => v.OptionValues)
                    .ThenInclude(vo => vo.OptionValue)
            .Include(p => p.AttributeValues)
                .ThenInclude(av => av.Attribute)
            .Include(p => p.AttributeValues)
                .ThenInclude(av => av.Option)
            .Include(p => p.Takedowns)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.Products
            .Include(p => p.Shop)
                .ThenInclude(s => s!.OwnerUser)
            .Include(p => p.Shop)
                .ThenInclude(s => s!.Sanctions)
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .Include(p => p.Options.OrderBy(o => o.Position))
                .ThenInclude(o => o.Values.OrderBy(v => v.Position))
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
                .ThenInclude(v => v.OptionValues)
                    .ThenInclude(vo => vo.OptionValue)
            .Include(p => p.AttributeValues)
                .ThenInclude(av => av.Attribute)
            .Include(p => p.AttributeValues)
                .ThenInclude(av => av.Option)
            .Include(p => p.Takedowns)
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug && p.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.Products.AnyAsync(p => p.Slug == normalizedSlug && p.DeletedAtUtc == null, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        if (_context.Entry(product).State == EntityState.Detached)
        {
            _context.Products.Update(product);
        }
    }

    public async Task AddImageAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ProductImages.AddAsync(image, cancellationToken);
    }

    public void RemoveImage(ProductImage image)
    {
        _context.ProductImages.Remove(image);
    }


    public async Task<IReadOnlyList<Product>> GetShopProductsAsync(
        Guid shopId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
                .ThenInclude(v => v.OptionValues)
                    .ThenInclude(ov => ov.OptionValue)
            .Where(p => p.ShopId == shopId && p.DeletedAtUtc == null);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Slug.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetShopProductsCountAsync(
        Guid shopId,
        ProductStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.ShopId == shopId && p.DeletedAtUtc == null);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Slug.ToLower().Contains(s));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetPublicCatalogAsync(
        Guid? categoryId,
        string? search,
        long? minPrice,
        long? maxPrice,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        var query = _context.Products
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
            .Where(p => p.DeletedAtUtc == null && p.Status == ProductStatus.Listed)
            .Where(p => !p.Takedowns.Any(t => t.LiftedAtUtc == null))
            // Shop phải Active (Open, Approved, không có Sanction còn hiệu lực)
            .Where(p => p.Shop != null && p.Shop.DeletedAtUtc == null &&
                        p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                        p.Shop.OperatingStatus == OperatingStatus.Open &&
                        !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
            // Danh mục phải Active
            .Where(p => p.Category == null || p.Category.IsActive);

        if (categoryId.HasValue)
        {
            var targetCategory = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value, cancellationToken);

            if (targetCategory != null)
            {
                var targetPath = targetCategory.Path;
                var descendantCategoryIds = await _context.Categories.AsNoTracking()
                    .Where(c => c.Id == targetCategory.Id 
                             || (!string.IsNullOrEmpty(targetPath) && c.Path.StartsWith(targetPath))
                             || c.ParentId == targetCategory.Id)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);

                query = query.Where(p => p.CategoryId.HasValue && descendantCategoryIds.Contains(p.CategoryId.Value));
            }
            else
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            query = query.Where(p => p.MaxPriceCache >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(p => p.MinPriceCache <= maxPrice.Value);
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.MinPriceCache),
            "price_desc" => query.OrderByDescending(p => p.MinPriceCache),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPublicCatalogCountAsync(
        Guid? categoryId,
        string? search,
        long? minPrice,
        long? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        var query = _context.Products
            .Where(p => p.DeletedAtUtc == null && p.Status == ProductStatus.Listed)
            .Where(p => !p.Takedowns.Any(t => t.LiftedAtUtc == null))
            .Where(p => p.Shop != null && p.Shop.DeletedAtUtc == null &&
                        p.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                        p.Shop.OperatingStatus == OperatingStatus.Open &&
                        !p.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
            .Where(p => p.Category == null || p.Category.IsActive);

        if (categoryId.HasValue)
        {
            var targetCategory = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value, cancellationToken);

            if (targetCategory != null)
            {
                var targetPath = targetCategory.Path;
                var descendantCategoryIds = await _context.Categories.AsNoTracking()
                    .Where(c => c.Id == targetCategory.Id 
                             || (!string.IsNullOrEmpty(targetPath) && c.Path.StartsWith(targetPath))
                             || c.ParentId == targetCategory.Id)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);

                query = query.Where(p => p.CategoryId.HasValue && descendantCategoryIds.Contains(p.CategoryId.Value));
            }
            else
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            query = query.Where(p => p.MaxPriceCache >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(p => p.MinPriceCache <= maxPrice.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetShopPublicProductsAsync(
        Guid shopId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
            .Where(p => p.ShopId == shopId && p.DeletedAtUtc == null && p.Status == ProductStatus.Listed)
            .Where(p => !p.Takedowns.Any(t => t.LiftedAtUtc == null));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetShopPublicProductsCountAsync(
        Guid shopId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.ShopId == shopId && p.DeletedAtUtc == null && p.Status == ProductStatus.Listed)
            .Where(p => !p.Takedowns.Any(t => t.LiftedAtUtc == null));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAdminProductsAsync(
        Guid? shopId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.Takedowns)
            .Include(p => p.Variants.Where(v => v.DeletedAtUtc == null))
            .Where(p => p.DeletedAtUtc == null);

        if (shopId.HasValue)
        {
            query = query.Where(p => p.ShopId == shopId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Slug.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAdminProductsCountAsync(
        Guid? shopId,
        ProductStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.DeletedAtUtc == null);

        if (shopId.HasValue)
        {
            query = query.Where(p => p.ShopId == shopId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Slug.ToLower().Contains(s));
        }

        return await query.CountAsync(cancellationToken);
    }
}

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly SocialDbContext _context;

    public ProductVariantRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.OptionValues)
                .ThenInclude(vo => vo.OptionValue)
            .FirstOrDefaultAsync(v => v.Id == id && v.DeletedAtUtc == null, cancellationToken);
    }

    public async Task<bool> ExistsBySkuInShopAsync(
        Guid shopId,
        string skuNormalized,
        Guid? excludeVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants
            .Where(v => v.ShopId == shopId && v.SkuNormalized == skuNormalized && v.DeletedAtUtc == null);

        if (excludeVariantId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVariantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .Include(v => v.OptionValues)
                .ThenInclude(vo => vo.OptionValue)
            .Where(v => v.ProductId == productId && v.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariants.AddAsync(variant, cancellationToken);
    }

    public void Update(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }
}
