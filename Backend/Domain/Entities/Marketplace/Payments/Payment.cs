using Domain.Common;
using Domain.Entities.Marketplace.Orders;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Payments;

/// <summary>
/// Thực thể ghi nhận phiên giao dịch thanh toán từ các cổng thanh toán (PayOS, SePay, VietQR, Mock)
/// </summary>
public class Payment : BaseEntity
{
    public Guid CheckoutId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string ProviderTxnId { get; private set; } = string.Empty;
    public string Method { get; private set; } = "QR";
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Initiated;
    public DateTime? PaidAt { get; private set; }
    public string? RawPayloadRef { get; private set; }
    public string? QrCodeUrl { get; private set; }
    public string? PaymentUrl { get; private set; }

    // Navigation
    public Checkout? Checkout { get; private set; }

    protected Payment() { }

    public Payment(
        Guid checkoutId,
        PaymentProvider provider,
        string providerTxnId,
        long amount,
        string method = "QR",
        string currency = "VND",
        string? qrCodeUrl = null,
        string? paymentUrl = null)
    {
        if (checkoutId == Guid.Empty) throw new ArgumentException("CheckoutId không hợp lệ.", nameof(checkoutId));
        if (string.IsNullOrWhiteSpace(providerTxnId)) throw new ArgumentException("Mã giao dịch từ cổng không được để trống.", nameof(providerTxnId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền thanh toán phải lớn hơn 0.");

        CheckoutId = checkoutId;
        Provider = provider;
        ProviderTxnId = providerTxnId.Trim();
        Amount = amount;
        Method = string.IsNullOrWhiteSpace(method) ? "QR" : method.Trim();
        Currency = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        Status = PaymentStatus.Initiated;
        QrCodeUrl = qrCodeUrl;
        PaymentUrl = paymentUrl;
    }

    public void MarkSucceeded(DateTime paidAt, string? rawPayloadRef = null)
    {
        if (Status == PaymentStatus.Succeeded) return;
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt;
        if (!string.IsNullOrWhiteSpace(rawPayloadRef))
        {
            RawPayloadRef = rawPayloadRef;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string? rawPayloadRef = null)
    {
        if (Status == PaymentStatus.Succeeded)
            throw new InvalidOperationException("Không thể đánh dấu thất bại cho giao dịch đã thanh toán thành công.");

        Status = PaymentStatus.Failed;
        if (!string.IsNullOrWhiteSpace(rawPayloadRef))
        {
            RawPayloadRef = rawPayloadRef;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status == PaymentStatus.Succeeded) return;
        Status = PaymentStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
