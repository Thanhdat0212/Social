using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.External.Caching;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Products;

public class CatalogService : ICatalogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CatalogService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<CatalogProductCardDto>> GetCatalogProductsAsync(
        CatalogFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page > 0 ? query.Page : 1;
        var pageSize = query.PageSize > 0 ? query.PageSize : 20;

        var products = await _unitOfWork.Products.GetPublicCatalogAsync(
            query.CategoryId,
            query.Search,
            query.MinPrice,
            query.MaxPrice,
            query.Sort,
            page,
            pageSize,
            cancellationToken);

        var totalCount = await _unitOfWork.Products.GetPublicCatalogCountAsync(
            query.CategoryId,
            query.Search,
            query.MinPrice,
            query.MaxPrice,
            cancellationToken);

        var items = products.Select(p => new CatalogProductCardDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ShopId = p.ShopId,
            ShopName = p.Shop?.Name ?? string.Empty,
            ShopSlug = p.Shop?.Slug ?? string.Empty,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            CoverImageUrl = p.Images.FirstOrDefault(i => i.IsCover)?.Url ?? p.Images.FirstOrDefault()?.Url,
            MinPrice = p.MinPriceCache,
            MaxPrice = p.MaxPriceCache,
            RatingAverage = p.RatingAverage,
            ReviewCount = p.ReviewCount,
            CreatedAtUtc = p.CreatedAtUtc
        }).ToList();

        return new PagedResult<CatalogProductCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CatalogProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ProductDetail(slug);
        var cached = await _cacheService.GetAsync<CatalogProductDetailDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var product = await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken);
        if (product == null) return null;

        // Kiểm tra tính hiển thị công khai (IsPubliclyVisible)
        var nowUtc = DateTime.UtcNow;
        var isVisible = product.Status == ProductStatus.Listed &&
                        !product.Takedowns.Any(t => t.LiftedAtUtc == null) &&
                        product.Shop != null &&
                        product.Shop.DeletedAtUtc == null &&
                        product.Shop.ApprovalStatus == ApprovalStatus.Approved &&
                        product.Shop.OperatingStatus == OperatingStatus.Open &&
                        !product.Shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)) &&
                        (product.Category == null || product.Category.IsActive);

        if (!isVisible)
        {
            return null;
        }

        var variantDtos = new List<ProductVariantDto>();
        foreach (var v in product.Variants.Where(v => v.DeletedAtUtc == null))
        {
            var labelParts = v.OptionValues.Select(ov => ov.OptionValue?.Value ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
            var variantName = labelParts.Count > 0 ? string.Join(" / ", labelParts) : (string.IsNullOrWhiteSpace(v.Sku) ? "Bản tiêu chuẩn" : v.Sku);

            var stockSummary = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(v.Id, cancellationToken);

            variantDtos.Add(new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                CombinationKey = v.CombinationKey,
                Name = variantName,
                Price = v.Price,
                CompareAtPrice = v.CompareAtPrice,
                DeliveryType = v.DeliveryType.ToString(),
                TrackInventory = v.TrackInventory,
                AvailableStock = stockSummary.Available,
                OptionValues = labelParts
            });
        }

        var detail = new CatalogProductDetailDto
        {
            Id = product.Id,
            ShopId = product.ShopId,
            ShopName = product.Shop?.Name ?? string.Empty,
            ShopSlug = product.Shop?.Slug ?? string.Empty,
            ShopLogoUrl = product.Shop?.LogoUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            MinPrice = product.MinPriceCache,
            MaxPrice = product.MaxPriceCache,
            RatingAverage = product.RatingAverage,
            ReviewCount = product.ReviewCount,
            CreatedAtUtc = product.CreatedAtUtc,
            Options = product.Options.OrderBy(o => o.Position).Select(o => new ProductOptionDto
            {
                Id = o.Id,
                Name = o.Name,
                Position = o.Position,
                Values = o.Values.OrderBy(v => v.Position).Select(v => new ProductOptionValueDto
                {
                    Id = v.Id,
                    Value = v.Value,
                    Position = v.Position
                }).ToList()
            }).ToList(),
            Variants = variantDtos,
            Images = product.Images.OrderBy(i => i.Position).Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = i.Url,
                Position = i.Position,
                IsCover = i.IsCover
            }).ToList(),
            AttributeValues = product.AttributeValues.Select(av => new ProductAttributeValueDto
            {
                Id = av.Id,
                AttributeId = av.AttributeId,
                AttributeCode = av.Attribute?.Code ?? string.Empty,
                AttributeName = av.Attribute?.Name ?? string.Empty,
                DataType = av.Attribute?.DataType.ToString() ?? string.Empty,
                OptionId = av.OptionId,
                OptionValue = av.Option?.Value,
                TextValue = av.TextValue,
                NumberValue = av.NumberValue,
                BoolValue = av.BoolValue
            }).ToList()
        };

        // Cache 15 phút
        _cacheService.Set(cacheKey, detail, TimeSpan.FromMinutes(15));
        return detail;
    }

    public async Task<PagedResult<CatalogProductCardDto>> GetShopPublicProductsAsync(
        string shopSlug,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var p = page > 0 ? page : 1;
        var ps = pageSize > 0 ? pageSize : 20;

        var shop = await _unitOfWork.Shops.GetBySlugAsync(shopSlug, cancellationToken);
        if (shop == null || shop.ApprovalStatus != ApprovalStatus.Approved || shop.OperatingStatus != OperatingStatus.Open)
        {
            return new PagedResult<CatalogProductCardDto>
            {
                Items = new List<CatalogProductCardDto>(),
                TotalCount = 0,
                Page = p,
                PageSize = ps
            };
        }

        var products = await _unitOfWork.Products.GetShopPublicProductsAsync(shop.Id, search, p, ps, cancellationToken);
        var totalCount = await _unitOfWork.Products.GetShopPublicProductsCountAsync(shop.Id, search, cancellationToken);

        var items = products.Select(pr => new CatalogProductCardDto
        {
            Id = pr.Id,
            Name = pr.Name,
            Slug = pr.Slug,
            ShopId = shop.Id,
            ShopName = shop.Name,
            ShopSlug = shop.Slug,
            CategoryId = pr.CategoryId,
            CategoryName = pr.Category?.Name,
            CoverImageUrl = pr.Images.FirstOrDefault(i => i.IsCover)?.Url ?? pr.Images.FirstOrDefault()?.Url,
            MinPrice = pr.MinPriceCache,
            MaxPrice = pr.MaxPriceCache,
            CreatedAtUtc = pr.CreatedAtUtc
        }).ToList();

        return new PagedResult<CatalogProductCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }
}
