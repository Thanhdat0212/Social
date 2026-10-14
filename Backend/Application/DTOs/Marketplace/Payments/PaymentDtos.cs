using Domain.Enums;

namespace Application.DTOs.Marketplace.Payments;

public class CreatePaymentRequest
{
    public Guid CheckoutId { get; set; }
    public PaymentProvider Provider { get; set; } = PaymentProvider.PayOs;
}

public class PaymentLinkDto
{
    public Guid PaymentId { get; set; }
    public Guid CheckoutId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderTxnId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? QrCodeUrl { get; set; }
    public string? PaymentUrl { get; set; }
    public string? PaymentCode { get; set; }
    public string? Description { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public Guid CheckoutId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderTxnId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? PaymentUrl { get; set; }
    public string? PaymentCode { get; set; }
    public string? Description { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WebhookProcessResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public Guid? CheckoutId { get; set; }
}
