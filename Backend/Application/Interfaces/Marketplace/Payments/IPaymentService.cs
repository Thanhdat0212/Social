using Application.DTOs.Marketplace.Payments;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Payments;

public interface IPaymentService
{
    /// <summary>
    /// Khởi tạo phiên thanh toán cho một Checkout (gọi gateway, lưu bản ghi Payment)
    /// </summary>
    Task<PaymentLinkDto> InitiatePaymentAsync(
        Guid userId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin thanh toán hiện tại của phiên Checkout
    /// </summary>
    Task<PaymentDetailDto?> GetPaymentByCheckoutIdAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý Webhook gửi từ cổng thanh toán (Verify chữ ký -> Ghi sổ cái -> Hoàn tất Checkout)
    /// </summary>
    Task<WebhookProcessResult> HandleWebhookAsync(
        PaymentProvider provider,
        string webhookBody,
        string signature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Fallback khi người mua bấm 'Tôi đã thanh toán' để đối soát trạng thái trực tiếp
    /// </summary>
    Task<PaymentDetailDto> VerifyAndSyncPaymentAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default);
}
