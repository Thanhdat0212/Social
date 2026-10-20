using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Infrastructure.External.Caching;
using Infrastructure.Marketplace.Shops;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Products;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    private async Task<Shop> GetRequiredShopAsync(Guid userId, CancellationToken cancellationToken)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(userId, cancellationToken);
        if (shop == null)
        {
            throw new KeyNotFoundException("Bạn chưa đăng ký gian hàng trên hệ thống.");
        }
        return shop;
    }

    private async Task<string> GenerateUniqueProductSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.GenerateSlug(name);
        var slug = baseSlug;
        var counter = 1;

        while (await _unitOfWork.Products.ExistsBySlugAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    public async Task<ProductDetailDto> CreateDraftAsync(Guid userId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);

        if (request.CategoryId.HasValue)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value, cancellationToken);
            if (category == null || !category.IsActive)
            {
                throw new ArgumentException("Danh mục được chọn không tồn tại hoặc đã bị vô hiệu hóa.");
            }
        }

        var slug = await GenerateUniqueProductSlugAsync(request.Name, cancellationToken);
        var product = new Product(shop.Id, request.Name, slug, request.CategoryId, request.Description);

        // Tạo biến thể mặc định nếu có thông tin giá
        if (request.DefaultVariant != null && request.DefaultVariant.Price > 0)
        {
            var sku = string.IsNullOrWhiteSpace(request.DefaultVariant.Sku)
                ? $"P{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
                : request.DefaultVariant.Sku.Trim();

            var skuNormalized = sku.ToUpperInvariant();
            if (await _unitOfWork.ProductVariants.ExistsBySkuInShopAsync(shop.Id, skuNormalized, null, cancellationToken))
            {
                throw new InvalidOperationException($"Mã SKU '{sku}' đã tồn tại trong gian hàng của bạn.");
            }

            var variant = new ProductVariant(
                product.Id,
                shop.Id,
                sku,
                "default",
                request.DefaultVariant.Price,
                request.DefaultVariant.CompareAtPrice,
                request.DefaultVariant.DeliveryType,
                request.DefaultVariant.TrackInventory);

            product.Variants.Add(variant);
            product.UpdatePriceCache(variant.Price, variant.Price);
        }

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop {ShopId} đã tạo bản nháp sản phẩm {ProductId} ({ProductName})", shop.Id, product.Id, product.Name);
        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductDetailDto> UpdateDetailsAsync(Guid userId, Guid productId, UpdateProductDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        if (request.CategoryId.HasValue && request.CategoryId != product.CategoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value, cancellationToken);
            if (category == null || !category.IsActive)
            {
                throw new ArgumentException("Danh mục được chọn không tồn tại hoặc đã bị vô hiệu hóa.");
            }
        }

        var slug = product.Slug;
        if (!string.Equals(product.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            slug = await GenerateUniqueProductSlugAsync(request.Name, cancellationToken);
        }

        var oldSlug = product.Slug;
        product.UpdateDetails(request.Name, slug, request.Description, request.CategoryId);

        if (request.Price.HasValue && request.Price.Value > 0)
        {
            var activeVariants = product.Variants.Where(v => v.DeletedAtUtc == null).ToList();
            if (activeVariants.Count == 1)
            {
                var singleVar = activeVariants[0];
                singleVar.Update(request.Price.Value, request.CompareAtPrice, singleVar.DeliveryType, singleVar.TrackInventory);
                product.UpdatePriceCache(singleVar.Price, singleVar.Price);
            }
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheService.Remove(CacheKeys.ProductDetail(oldSlug));
        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductDetailDto> UpdateVariantPriceAsync(Guid userId, Guid productId, Guid variantId, UpdateVariantPriceRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == variantId && v.DeletedAtUtc == null);
        if (variant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm cần cập nhật giá.");
        }

        variant.Update(request.Price, request.CompareAtPrice, variant.DeliveryType, variant.TrackInventory);

        var activeVariants = product.Variants.Where(v => v.DeletedAtUtc == null).ToList();
        long minPrice = activeVariants.Count > 0 ? activeVariants.Min(v => v.Price) : 0;
        long maxPrice = activeVariants.Count > 0 ? activeVariants.Max(v => v.Price) : 0;
        product.UpdatePriceCache(minPrice, maxPrice);

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        var resultDto = MapToDetailDto(product, shop.Name, shop.Slug);
        foreach (var vDto in resultDto.Variants)
        {
            var stock = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(vDto.Id, cancellationToken);
            vDto.AvailableStock = stock.Available;
        }

        return resultDto;
    }

    public async Task<ProductDetailDto> UpdateVariantsAsync(Guid userId, Guid productId, UpdateProductVariantsRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        if (request.Variants == null || request.Variants.Count == 0)
        {
            throw new ArgumentException("Sản phẩm phải có ít nhất một biến thể.");
        }

        // 1. Cập nhật các trục Options
        product.Options.Clear();
        int optPos = 0;
        var createdOptionsMap = new Dictionary<string, (ProductOption Option, Dictionary<string, ProductOptionValue> ValuesMap)>();

        foreach (var optInput in request.Options)
        {
            var option = new ProductOption(product.Id, optInput.Name, optPos++);
            var valuesMap = new Dictionary<string, ProductOptionValue>();
            int valPos = 0;
            foreach (var valStr in optInput.Values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var optVal = new ProductOptionValue(option.Id, valStr, valPos++);
                option.Values.Add(optVal);
                valuesMap[valStr.ToLowerInvariant()] = optVal;
            }
            product.Options.Add(option);
            createdOptionsMap[optInput.Name.ToLowerInvariant()] = (option, valuesMap);
        }

        // 2. Cập nhật các biến thể Variants
        // Đánh dấu các variant cũ đã bị loại bỏ thành soft-delete
        var existingVariants = product.Variants.Where(v => v.DeletedAtUtc == null).ToList();
        var incomingVariantIds = request.Variants.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();

        foreach (var oldVar in existingVariants)
        {
            if (!incomingVariantIds.Contains(oldVar.Id))
            {
                oldVar.SoftDelete();
            }
        }

        long minPrice = long.MaxValue;
        long maxPrice = 0;
        int varIndex = 1;

        foreach (var vInput in request.Variants)
        {
            var sku = string.IsNullOrWhiteSpace(vInput.Sku)
                ? $"P{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
                : vInput.Sku.Trim();

            var skuNormalized = sku.ToUpperInvariant();

            // Kiểm tra SKU không trùng trong shop
            if (await _unitOfWork.ProductVariants.ExistsBySkuInShopAsync(shop.Id, skuNormalized, vInput.Id, cancellationToken))
            {
                throw new InvalidOperationException($"Mã SKU '{sku}' đã được sử dụng bởi một biến thể khác trong gian hàng của bạn.");
            }

            // Tạo CombinationKey ổn định (sắp xếp theo alphabet)
            var sortedOptionVals = vInput.OptionValues
                .Where(ov => !string.IsNullOrWhiteSpace(ov))
                .Select(ov => ov.Trim().ToLowerInvariant())
                .OrderBy(ov => ov)
                .ToList();

            var combinationKey = sortedOptionVals.Count > 0 ? string.Join("|", sortedOptionVals) : $"var-{varIndex++}";

            ProductVariant variant;
            if (vInput.Id.HasValue && existingVariants.FirstOrDefault(ev => ev.Id == vInput.Id.Value) is { } matched)
            {
                matched.Update(vInput.Price, vInput.CompareAtPrice, vInput.DeliveryType, vInput.TrackInventory);
                matched.UpdateSku(sku);
                variant = matched;
            }
            else
            {
                variant = new ProductVariant(
                    product.Id,
                    shop.Id,
                    sku,
                    combinationKey,
                    vInput.Price,
                    vInput.CompareAtPrice,
                    vInput.DeliveryType,
                    vInput.TrackInventory);

                product.Variants.Add(variant);
            }

            // Gán Option Values cho Variant
            variant.OptionValues.Clear();
            foreach (var optValStr in vInput.OptionValues)
            {
                foreach (var optGroup in createdOptionsMap.Values)
                {
                    if (optGroup.ValuesMap.TryGetValue(optValStr.ToLowerInvariant(), out var optValEntity))
                    {
                        variant.OptionValues.Add(new VariantOptionValue(variant.Id, optValEntity.Id));
                    }
                }
            }

            if (variant.Price < minPrice) minPrice = variant.Price;
            if (variant.Price > maxPrice) maxPrice = variant.Price;
        }

        if (minPrice == long.MaxValue) minPrice = 0;
        product.UpdatePriceCache(minPrice, maxPrice);

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        var resultDto = MapToDetailDto(product, shop.Name, shop.Slug);
        foreach (var vDto in resultDto.Variants)
        {
            var stock = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(vDto.Id, cancellationToken);
            vDto.AvailableStock = stock.Available;
        }

        return resultDto;
    }

    public async Task<ProductDetailDto> SaveAttributesAsync(Guid userId, Guid productId, SaveProductAttributeValuesRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        product.AttributeValues.Clear();
        foreach (var attrInput in request.Attributes)
        {
            var attrVal = new ProductAttributeValue(
                product.Id,
                attrInput.AttributeId,
                attrInput.OptionId,
                attrInput.TextValue,
                attrInput.NumberValue,
                attrInput.BoolValue);

            product.AttributeValues.Add(attrVal);
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));

        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductImageDto> AddImageAsync(Guid userId, Guid productId, UploadProductImageRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        var isCover = request.IsCover || product.Images.Count == 0;
        if (isCover)
        {
            foreach (var img in product.Images)
            {
                img.SetCover(false);
            }
        }

        var newImage = new ProductImage(product.Id, request.Url, request.Position > 0 ? request.Position : product.Images.Count, isCover);
        await _unitOfWork.Products.AddImageAsync(newImage, cancellationToken);
        product.Images.Add(newImage);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return new ProductImageDto
        {
            Id = newImage.Id,
            Url = newImage.Url,
            Position = newImage.Position,
            IsCover = newImage.IsCover
        };
    }

    public async Task DeleteImageAsync(Guid userId, Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return;

        var wasCover = image.IsCover;
        product.Images.Remove(image);
        _unitOfWork.Products.RemoveImage(image);

        if (wasCover && product.Images.Count > 0)
        {
            product.Images.First().SetCover(true);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));
    }

    public async Task ReorderImagesAsync(Guid userId, Guid productId, ReorderProductImagesRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        foreach (var orderItem in request.Images)
        {
            var img = product.Images.FirstOrDefault(i => i.Id == orderItem.ImageId);
            if (img != null)
            {
                img.UpdatePosition(orderItem.Position, orderItem.IsCover);
            }
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));
    }

    public async Task<ProductDetailDto> PublishAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        // Quy tắc kiểm tra trước khi Publish (M3.3):
        // 1. Phải có CategoryId
        if (!product.CategoryId.HasValue)
        {
            throw new InvalidOperationException("Sản phẩm phải được gán vào một danh mục cụ thể trước khi đăng bán.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId.Value, cancellationToken);
        if (category == null || !category.IsActive)
        {
            throw new InvalidOperationException("Danh mục của sản phẩm không khả dụng hoặc đã bị vô hiệu hóa.");
        }

        // 2. Phải có ít nhất 1 biến thể với giá > 0
        var activeVariants = product.Variants.Where(v => v.DeletedAtUtc == null).ToList();
        if (activeVariants.Count == 0 || activeVariants.Any(v => v.Price <= 0))
        {
            throw new InvalidOperationException("Sản phẩm phải có ít nhất 1 biến thể với giá bán hợp lệ (> 0).");
        }

        // 3. Phải có ít nhất 1 hình ảnh
        if (product.Images.Count == 0)
        {
            throw new InvalidOperationException("Sản phẩm phải có ít nhất 1 hình ảnh trước khi đăng bán.");
        }

        // 4. Kiểm tra thuộc tính bắt buộc của danh mục
        var categoryAttributes = await _unitOfWork.Attributes.GetCategoryAttributesAsync(category.Id, cancellationToken);
        var requiredAttrs = categoryAttributes.Where(ca => ca.Requirement == AttributeRequirement.Required).ToList();

        foreach (var req in requiredAttrs)
        {
            var hasVal = product.AttributeValues.Any(av =>
                av.AttributeId == req.AttributeId &&
                (!string.IsNullOrWhiteSpace(av.TextValue) || av.OptionId.HasValue || av.NumberValue.HasValue || av.BoolValue.HasValue));

            if (!hasVal)
            {
                throw new InvalidOperationException($"Sản phẩm chưa điền thuộc tính bắt buộc: '{req.AttributeDefinition?.Name ?? req.AttributeId.ToString()}'.");
            }
        }

        var requiresReview = category.ModerationPolicy == CategoryModerationPolicy.RequiresReview;
        product.Publish(requiresReview);

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        _logger.LogInformation("Sản phẩm {ProductId} đã được đăng bán (Status: {Status})", product.Id, product.Status);
        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductDetailDto> HideAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        product.Hide();
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductDetailDto> UnhideAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        product.Unhide();
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<ProductDetailDto> ArchiveAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa.");
        }

        product.Archive();
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.Remove(CacheKeys.ProductDetail(product.Slug));
        _cacheService.Remove(CacheKeys.ShopPublic(shop.Slug));

        return MapToDetailDto(product, shop.Name, shop.Slug);
    }

    public async Task<PagedResult<ProductListItemDto>> GetShopProductsAsync(
        Guid userId,
        ProductStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var products = await _unitOfWork.Products.GetShopProductsAsync(shop.Id, status, search, page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Products.GetShopProductsCountAsync(shop.Id, status, search, cancellationToken);

        var items = new List<ProductListItemDto>();
        foreach (var p in products)
        {
            var variantDtos = new List<ProductVariantDto>();
            foreach (var v in p.Variants.Where(v => v.DeletedAtUtc == null))
            {
                var labelParts = v.OptionValues.Select(ov => ov.OptionValue?.Value ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var variantName = labelParts.Count > 0 ? string.Join(" / ", labelParts) : (string.IsNullOrWhiteSpace(v.Sku) ? "Gói Tiêu Chuẩn" : v.Sku);
                var stock = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(v.Id, cancellationToken);
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
                    AvailableStock = stock.Available,
                    OptionValues = labelParts
                });
            }

            items.Add(new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                CoverImageUrl = p.Images.FirstOrDefault(i => i.IsCover)?.Url ?? p.Images.FirstOrDefault()?.Url,
                Status = p.Status.ToString(),
                MinPrice = p.MinPriceCache,
                MaxPrice = p.MaxPriceCache,
                VariantCount = variantDtos.Count,
                Variants = variantDtos,
                CreatedAtUtc = p.CreatedAtUtc
            });
        }

        return new PagedResult<ProductListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetRequiredShopAsync(userId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdWithDetailsAsync(productId, cancellationToken);

        if (product == null || product.ShopId != shop.Id)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc bạn không có quyền xem thông tin.");
        }

        var dto = MapToDetailDto(product, shop.Name, shop.Slug);
        foreach (var vDto in dto.Variants)
        {
            var stock = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(vDto.Id, cancellationToken);
            vDto.AvailableStock = stock.Available;
        }

        return dto;
    }

    private static ProductDetailDto MapToDetailDto(Product product, string shopName, string shopSlug)
    {
        var activeTakedown = product.Takedowns.FirstOrDefault(t => t.LiftedAtUtc == null);

        return new ProductDetailDto
        {
            Id = product.Id,
            ShopId = product.ShopId,
            ShopName = shopName,
            ShopSlug = shopSlug,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Status = product.Status.ToString(),
            MinPrice = product.MinPriceCache,
            MaxPrice = product.MaxPriceCache,
            HasActiveTakedown = activeTakedown != null,
            ActiveTakedownReason = activeTakedown?.Reason,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
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
            Variants = product.Variants.Where(v => v.DeletedAtUtc == null).Select(v =>
            {
                var labelParts = v.OptionValues.Select(ov => ov.OptionValue?.Value ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var variantName = labelParts.Count > 0 ? string.Join(" / ", labelParts) : (string.IsNullOrWhiteSpace(v.Sku) ? "Bản tiêu chuẩn" : v.Sku);
                return new ProductVariantDto
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    CombinationKey = v.CombinationKey,
                    Name = variantName,
                    Price = v.Price,
                    CompareAtPrice = v.CompareAtPrice,
                    DeliveryType = v.DeliveryType.ToString(),
                    TrackInventory = v.TrackInventory,
                    OptionValues = labelParts
                };
            }).ToList(),
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
    }
}
