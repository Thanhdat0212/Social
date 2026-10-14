using Application.DTOs.Marketplace.Orders;
using Application.Interfaces.Marketplace.Orders;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Orders;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken);
        if (cart == null)
        {
            cart = new Cart(userId);
            await _unitOfWork.Carts.AddCartAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return cart;
    }

    public async Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        return await MapToCartDtoAsync(cart, userId, cancellationToken);
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.VariantId, cancellationToken);
        if (variant == null || variant.DeletedAtUtc.HasValue)
        {
            throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm yêu cầu.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(variant.ProductId, cancellationToken);
        if (product == null || product.Status != ProductStatus.Listed || product.DeletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Sản phẩm này hiện không còn kinh doanh trên sàn.");
        }

        // Chặn người bán tự mua hàng của chính gian hàng mình
        var shop = await _unitOfWork.Shops.GetByIdAsync(variant.ShopId, cancellationToken);
        if (shop != null && shop.OwnerUserId == userId)
        {
            throw new InvalidOperationException("Bạn không thể mua sản phẩm của chính gian hàng mình.");
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId);
        if (existingItem != null)
        {
            var newQty = Math.Min(existingItem.Quantity + request.Quantity, 100);
            existingItem.UpdateQuantity(newQty);
        }
        else
        {
            var newItem = new CartItem(cart.Id, variant.Id, request.Quantity);
            await _unitOfWork.Carts.AddItemAsync(newItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload cart để có đầy đủ navigation properties
        var reloadedCart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken);
        return await MapToCartDtoAsync(reloadedCart ?? cart, userId, cancellationToken);
    }

    public async Task<CartDto> UpdateItemQuantityAsync(Guid userId, Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm trong giỏ hàng.");
        }

        item.UpdateQuantity(request.Quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reloadedCart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken);
        return await MapToCartDtoAsync(reloadedCart ?? cart, userId, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _unitOfWork.Carts.RemoveItem(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var reloadedCart = await _unitOfWork.Carts.GetByUserIdAsync(userId, cancellationToken);
        return await MapToCartDtoAsync(reloadedCart ?? cart, userId, cancellationToken);
    }

    public async Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        await _unitOfWork.Carts.ClearCartAsync(cart.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<CartDto> MapToCartDtoAsync(Cart cart, Guid currentUserId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var itemDtos = new List<CartItemDto>();

        foreach (var item in cart.Items)
        {
            var variant = item.Variant;
            var product = variant?.Product;
            var shop = product?.Shop;

            bool isAvailable = true;
            string? reason = null;
            int stock = 0;

            if (variant != null)
            {
                var summary = await _unitOfWork.DigitalCodes.GetInventorySummaryAsync(variant.Id, cancellationToken);
                stock = summary.Available;
            }

            if (variant == null || variant.DeletedAtUtc.HasValue)
            {
                isAvailable = false;
                reason = "Biến thể không tồn tại hoặc đã ngừng bán";
            }
            else if (product == null || product.Status != ProductStatus.Listed || product.DeletedAtUtc.HasValue || product.HasActiveTakedown())
            {
                isAvailable = false;
                reason = "Sản phẩm hiện không khả dụng trên sàn";
            }
            else if (shop == null || shop.DeletedAtUtc.HasValue ||
                     shop.ApprovalStatus != ApprovalStatus.Approved ||
                     shop.OperatingStatus != OperatingStatus.Open ||
                     shop.Sanctions.Any(sn => sn.LiftedAtUtc == null && (sn.EndAtUtc == null || sn.EndAtUtc > nowUtc)))
            {
                isAvailable = false;
                reason = "Gian hàng hiện đang tạm đóng hoặc bị xử phạt";
            }
            else if (shop.OwnerUserId == currentUserId)
            {
                isAvailable = false;
                reason = "Không thể mua sản phẩm của chính gian hàng mình";
            }
            else if (variant.TrackInventory && stock <= 0)
            {
                isAvailable = false;
                reason = "Sản phẩm tạm thời hết hàng trong kho";
            }
            else if (variant.TrackInventory && item.Quantity > stock)
            {
                isAvailable = false;
                reason = $"Kho chỉ còn {stock} mã khả dụng (bạn đang chọn {item.Quantity})";
            }

            var labelParts = variant?.OptionValues.Select(ov => ov.OptionValue?.Value).Where(v => !string.IsNullOrEmpty(v));
            var variantLabel = labelParts != null && labelParts.Any() ? string.Join(" / ", labelParts) : (string.IsNullOrWhiteSpace(variant?.Sku) ? "Bản tiêu chuẩn" : variant!.Sku);

            itemDtos.Add(new CartItemDto
            {
                Id = item.Id,
                VariantId = item.VariantId,
                ProductId = product?.Id ?? Guid.Empty,
                ProductName = product?.Name ?? "Sản phẩm không khả dụng",
                VariantLabel = variantLabel,
                Sku = variant?.Sku ?? string.Empty,
                ShopId = shop?.Id ?? Guid.Empty,
                ShopName = shop?.Name ?? string.Empty,
                ShopSlug = shop?.Slug ?? string.Empty,
                UnitPrice = variant?.Price ?? 0,
                Quantity = item.Quantity,
                IsAvailable = isAvailable,
                UnavailableReason = reason,
                AvailableStock = stock,
                CoverImageUrl = product?.Images.FirstOrDefault(i => i.IsCover)?.Url ?? product?.Images.FirstOrDefault()?.Url
            });
        }

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = itemDtos
        };
    }
}
