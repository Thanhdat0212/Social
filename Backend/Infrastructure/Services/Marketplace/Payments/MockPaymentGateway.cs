using Application.Interfaces.Marketplace.Payments;
using Domain.Enums;

namespace Infrastructure.Services.Marketplace.Payments;

public class MockPaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Mock;

    public Task<GatewayPaymentLinkResult> CreatePaymentLinkAsync(
        Guid checkoutId,
        string checkoutNo,
        long amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var txnId = $"MOCK-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var result = new GatewayPaymentLinkResult
        {
            ProviderTxnId = txnId,
            PaymentUrl = $"http://localhost:5173/checkout/{checkoutId}?mock=true",
            QrCodeUrl = $"https://api.vietqr.io/image/970422-000012345678-compact2.jpg?amount={amount}&addInfo={Uri.EscapeDataString(description)}",
            AccountNumber = "000012345678",
            AccountName = "CONG TY CP CONG NGHE SOCIAL (TEST)",
            Description = description,
            PaymentCode = txnId
        };

        return Task.FromResult(result);
    }

    public Task<GatewayVerifyResult> VerifyPaymentAsync(string providerTxnId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayVerifyResult
        {
            IsPaid = true,
            AmountPaid = 100000,
            Status = "PAID",
            PaidAt = DateTime.UtcNow,
            TransactionReference = providerTxnId
        });
    }

    public bool VerifyWebhookSignature(string webhookBody, string signature) => true;

    public string? ParseWebhookTransactionId(string webhookBody) => "MOCK-TXN";
}
