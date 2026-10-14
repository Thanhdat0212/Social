using Domain.Enums;

namespace Application.Interfaces.Marketplace.Payments;

public class GatewayPaymentLinkResult
{
    public string ProviderTxnId { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? Description { get; set; }
    public string? PaymentCode { get; set; }
}

public class GatewayVerifyResult
{
    public bool IsPaid { get; set; }
    public long AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
}

public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    /// <summary>
    /// Gọi tới cổng để tạo link thanh toán và mã QR
    /// </summary>
    Task<GatewayPaymentLinkResult> CreatePaymentLinkAsync(
        Guid checkoutId,
        string checkoutNo,
        long amount,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chủ động gọi API cổng để đối soát trạng thái giao dịch
    /// </summary>
    Task<GatewayVerifyResult> VerifyPaymentAsync(
        string providerTxnId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác thực chữ ký HMAC SHA256 của Webhook từ cổng gửi về
    /// </summary>
    bool VerifyWebhookSignature(string webhookBody, string signature);

    /// <summary>
    /// Trích xuất mã giao dịch (orderCode / txnId) từ nội dung webhook
    /// </summary>
    string? ParseWebhookTransactionId(string webhookBody);
}
