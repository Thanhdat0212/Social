using Infrastructure.Persistence;
using Application.Interfaces.Repositories.Marketplace.Orders;
using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Marketplace.Orders;

public class CartRepository : ICartRepository
{
    private readonly SocialDbContext _context;

    public CartRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p!.Shop)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.OptionValues)
                        .ThenInclude(vo => vo.OptionValue)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<CartItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _context.CartItems
            .Include(i => i.Cart)
            .Include(i => i.Variant)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
    }

    public async Task AddCartAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public async Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        await _context.CartItems.AddAsync(item, cancellationToken);
    }

    public void RemoveItem(CartItem item)
    {
        _context.CartItems.Remove(item);
    }

    public async Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var items = await _context.CartItems
            .Where(i => i.CartId == cartId)
            .ToListAsync(cancellationToken);

        _context.CartItems.RemoveRange(items);
    }

    public async Task RemoveItemsByVariantIdsAsync(Guid cartId, IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        var idList = variantIds.ToList();
        var items = await _context.CartItems
            .Where(i => i.CartId == cartId && idList.Contains(i.VariantId))
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _context.CartItems.RemoveRange(items);
        }
    }
}

public class CheckoutRepository : ICheckoutRepository
{
    private readonly SocialDbContext _context;

    public CheckoutRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Checkouts
            .Include(c => c.Items)
                .ThenInclude(i => i.Shop)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Checkout?> GetByNoAsync(string checkoutNo, CancellationToken cancellationToken = default)
    {
        var normalized = checkoutNo.Trim().ToUpperInvariant();
        return await _context.Checkouts
            .Include(c => c.Items)
                .ThenInclude(i => i.Shop)
            .FirstOrDefaultAsync(c => c.CheckoutNo == normalized, cancellationToken);
    }

    public async Task<Checkout?> GetByIdempotencyKeyAsync(Guid buyerUserId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Checkouts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerUserId == buyerUserId && c.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(Checkout checkout, CancellationToken cancellationToken = default)
    {
        await _context.Checkouts.AddAsync(checkout, cancellationToken);
    }

    public void Update(Checkout checkout)
    {
        _context.Checkouts.Update(checkout);
    }

    public async Task<IReadOnlyList<Checkout>> GetExpiredOpenCheckoutsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Checkouts
            .Include(c => c.Items)
            .Where(c => c.Status == CheckoutStatus.Open && c.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }
}

public class OrderRepository : IOrderRepository
{
    private readonly SocialDbContext _context;

    public OrderRepository(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<ShopOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.Shop)
            .Include(o => o.BuyerUser)
            .Include(o => o.Histories.OrderBy(h => h.CreatedAtUtc))
            .Include(o => o.Fulfillments)
                .ThenInclude(f => f.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<ShopOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        var normalized = orderNo.Trim().ToUpperInvariant();
        return await _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.Shop)
            .Include(o => o.Histories.OrderBy(h => h.CreatedAtUtc))
            .FirstOrDefaultAsync(o => o.OrderNo == normalized, cancellationToken);
    }

    public async Task<ShopOrder?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.Shop)
            .Include(o => o.BuyerUser)
            .FirstOrDefaultAsync(o => o.Items.Any(i => i.Id == orderItemId), cancellationToken);
    }

    public async Task<IReadOnlyList<ShopOrder>> GetOrdersByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.Shop)
            .Where(o => o.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShopOrder order, CancellationToken cancellationToken = default)
    {
        await _context.ShopOrders.AddAsync(order, cancellationToken);
    }

    public void Update(ShopOrder order)
    {
        _context.ShopOrders.Update(order);
    }

    public async Task<IReadOnlyList<ShopOrder>> GetBuyerOrdersAsync(
        Guid buyerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.Shop)
            .Where(o => o.BuyerUserId == buyerUserId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetBuyerOrdersCountAsync(
        Guid buyerUserId,
        OrderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShopOrders
            .Where(o => o.BuyerUserId == buyerUserId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShopOrder>> GetShopOrdersAsync(
        Guid shopId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShopOrders
            .Include(o => o.Items)
            .Include(o => o.BuyerUser)
            .Where(o => o.ShopId == shopId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetShopOrdersCountAsync(
        Guid shopId,
        OrderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShopOrders
            .Where(o => o.ShopId == shopId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
