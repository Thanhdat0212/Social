using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.External.Caching;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Products;

public class AdminProductService : IAdminProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminProductService> _logger;

    public AdminProductService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<AdminProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        Guid? shopId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page > 0 ? page : 1;
        var ps = pageSize > 0 ? pageSize : 20;

        var products = await _unitOfWork.Products.GetAdminProductsAsync(shopId, status, search, p, ps, cancellationToken);
        var totalCount = await _unitOfWork.Products.GetAdminProductsCountAsync(shopId, status, search, cancellationToken);

        var items = products.Select(pr => new ProductListItemDto
        {
            Id = pr.Id,
            Name = pr.Name,
            Slug = pr.Slug,
            CategoryId = pr.CategoryId,
            CategoryName = pr.Category?.Name,
            CoverImageUrl = pr.Images.FirstOrDefault(i => i.IsCover)?.Url ?? pr.Images.FirstOrDefault()?.Url,
            Status = pr.Status.ToString(),
            MinPrice = pr.MinPriceCache,
            MaxPrice = pr.MaxPriceCache,
            VariantCount = pr.Variants.Count(v => v.DeletedAtUtc == null),
            CreatedAtUtc = pr.CreatedAtUtc,
            HasActiveTakedown = pr.HasActiveTakedown(),
            ShopName = pr.Shop?.Name
        }).ToList();

        return new PagedResult<ProductListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }

    public async Task TakedownAsync(Guid adminId, Guid productId, string reason, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm cần gỡ bỏ.");
        }

        if (product.HasActiveTakedown())
        {
            throw new InvalidOperationException("Sản phẩm này hiện đang bị gỡ bỏ vi phạm.");
        }

        var takedown = new ProductTakedown(product.Id, adminId, reason);
        product.Takedowns.Add(takedown);

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        if (product.Shop != null)
        {
            _cacheService.Remove(CacheKeys.ShopPublic(product.Shop.Slug));
        }

        _logger.LogWarning("Admin {AdminId} đã gỡ bỏ sản phẩm {ProductId} với lý do: {Reason}", adminId, productId, reason);
    }

    public async Task LiftTakedownAsync(Guid adminId, Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm.");
        }

        var activeTakedown = product.Takedowns.FirstOrDefault(t => t.LiftedAtUtc == null);
        if (activeTakedown == null)
        {
            throw new InvalidOperationException("Sản phẩm không có lệnh gỡ bỏ nào đang hiệu lực.");
        }

        activeTakedown.Lift(adminId);

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        if (product.Shop != null)
        {
            _cacheService.Remove(CacheKeys.ShopPublic(product.Shop.Slug));
        }

        _logger.LogInformation("Admin {AdminId} đã dỡ bỏ lệnh khóa đối với sản phẩm {ProductId}", adminId, productId);
    }
}
