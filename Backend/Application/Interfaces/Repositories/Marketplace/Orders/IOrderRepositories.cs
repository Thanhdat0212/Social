using Domain.Entities.Marketplace.Orders;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Orders;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task AddCartAsync(Cart cart, CancellationToken cancellationToken = default);
    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);
    void RemoveItem(CartItem item);
    Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task RemoveItemsByVariantIdsAsync(Guid cartId, IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default);
}

public interface ICheckoutRepository
{
    Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Checkout?> GetByNoAsync(string checkoutNo, CancellationToken cancellationToken = default);
    Task<Checkout?> GetByIdempotencyKeyAsync(Guid buyerUserId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(Checkout checkout, CancellationToken cancellationToken = default);
    void Update(Checkout checkout);
    Task<IReadOnlyList<Checkout>> GetExpiredOpenCheckoutsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<ShopOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopOrder?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<ShopOrder?> GetByOrderItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopOrder>> GetOrdersByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken = default);
    Task AddAsync(ShopOrder order, CancellationToken cancellationToken = default);
    void Update(ShopOrder order);

    Task<IReadOnlyList<ShopOrder>> GetBuyerOrdersAsync(
        Guid buyerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetBuyerOrdersCountAsync(
        Guid buyerUserId,
        OrderStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShopOrder>> GetShopOrdersAsync(
        Guid shopId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetShopOrdersCountAsync(
        Guid shopId,
        OrderStatus? status,
        CancellationToken cancellationToken = default);
}
