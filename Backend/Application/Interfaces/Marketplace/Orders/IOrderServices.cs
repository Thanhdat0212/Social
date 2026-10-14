using Application.DTOs.Marketplace.Orders;
using Application.DTOs.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Orders;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemQuantityAsync(Guid userId, Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ICheckoutService
{
    /// <summary>
    /// Khởi tạo phiên Checkout và giữ mã trong kho (FOR UPDATE SKIP LOCKED)
    /// </summary>
    Task<CheckoutDetailDto> CreateCheckoutAsync(Guid userId, CreateCheckoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xem chi tiết phiên thanh toán
    /// </summary>
    Task<CheckoutDetailDto?> GetCheckoutByIdAsync(Guid userId, Guid checkoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý thanh toán thành công (dùng chung cho Mock payment và Cổng thanh toán thật sau này).
    /// Idempotent và an toàn khi gọi đồng thời: chỉ 1 lời gọi được sinh đơn hàng.
    /// Nếu phiên đã hết hạn: thử giữ lại mã; không đủ mã thì trả về RefundRequired = true.
    /// </summary>
    Task<CheckoutPaymentResult> CompleteCheckoutPaymentAsync(Guid checkoutId, string providerTxnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thanh toán thử nghiệm (chỉ môi trường Development)
    /// </summary>
    Task<MockPayResultDto> MockPayAsync(Guid userId, Guid checkoutId, MockPayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy / Giải phóng mã khi Checkout hết hạn
    /// </summary>
    Task ExpireCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<PagedResult<BuyerOrderDetailDto>> GetBuyerOrdersAsync(
        Guid buyerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BuyerOrderDetailDto?> GetBuyerOrderByIdAsync(
        Guid buyerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SellerOrderDetailDto>> GetShopOrdersAsync(
        Guid sellerUserId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SellerOrderDetailDto?> GetShopOrderByIdAsync(
        Guid sellerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<SignedDownloadUrlDto> GenerateSignedDownloadUrlAsync(
        Guid buyerUserId,
        Guid orderItemId,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task<(string storagePath, string fileName, string contentType)> ProcessFileDownloadAsync(
        Guid orderItemId,
        Guid buyerUserId,
        long expiresAt,
        string signature,
        CancellationToken cancellationToken = default);
}
