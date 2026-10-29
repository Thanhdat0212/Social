using Application.DTOs.Marketplace.Payments;
using Application.Interfaces.Marketplace.Payments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/webhooks/payments")]
[AllowAnonymous]
public class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentWebhooksController> _logger;

    public PaymentWebhooksController(
        IPaymentService paymentService,
        ILogger<PaymentWebhooksController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint tiếp nhận Webhook thanh toán từ cổng (PayOS, VietQR)
    /// Hỗ trợ cả các URL:
    /// - POST /api/webhooks/payments/PayOs
    /// - POST /api/payments/PayOs/webhook
    /// - POST /api/payments/payos/webhook
    /// - POST /api/webhooks/payos
    /// </summary>
    [HttpPost("{provider}")]
    [HttpPost("/api/payments/{provider}/webhook")]
    [HttpPost("/api/payments/payos/webhook")]
    [HttpPost("/api/webhooks/payos")]
    [ProducesResponseType(typeof(WebhookProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(
        PaymentProvider provider = PaymentProvider.PayOs,
        [FromHeader(Name = "x-signature")] string? signature = null,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Request.Body);
        var webhookBody = await reader.ReadToEndAsync(cancellationToken);

        _logger.LogInformation("Nhận webhook thanh toán từ {Provider} (Payload length: {PayloadLength} bytes, HasSignature: {HasSignature})", 
            provider, webhookBody.Length, !string.IsNullOrWhiteSpace(signature));

        var result = await _paymentService.HandleWebhookAsync(provider, webhookBody, signature ?? string.Empty, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
