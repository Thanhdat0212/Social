using Application.Common;
using Application.DTOs.Marketplace.Payments;
using Application.Interfaces.Marketplace.Payments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(
        IPaymentService paymentService,
        ICurrentUserService currentUserService)
    {
        _paymentService = paymentService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Khởi tạo phiên thanh toán cổng (PayOS VietQR) cho phiên Checkout
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.InitiatePaymentAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Khởi tạo thanh toán trực tiếp qua checkoutId (POST /api/payments/checkout/{checkoutId})
    /// </summary>
    [HttpPost("checkout/{checkoutId:guid}")]
    [ProducesResponseType(typeof(PaymentLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiatePaymentByCheckoutId(
        Guid checkoutId,
        [FromQuery] PaymentProvider provider = PaymentProvider.PayOs,
        CancellationToken cancellationToken = default)
    {
        var request = new CreatePaymentRequest { CheckoutId = checkoutId, Provider = provider };
        var result = await _paymentService.InitiatePaymentAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết thông tin thanh toán của phiên Checkout
    /// </summary>
    [HttpGet("checkout/{checkoutId:guid}")]
    [ProducesResponseType(typeof(PaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByCheckoutId(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentByCheckoutIdAsync(CurrentUserId, checkoutId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Chưa có thông tin thanh toán cho phiên checkout này." });
        }
        return Ok(result);
    }

    /// <summary>
    /// API Verification Fallback: Người mua bấm 'Tôi đã thanh toán' để chủ động đối soát từ PayOS khi webhook bị trễ
    /// </summary>
    [HttpGet("checkout/{checkoutId:guid}/verify")]
    [HttpPost("checkout/{checkoutId:guid}/verify")]
    [ProducesResponseType(typeof(PaymentDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPayment(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.VerifyAndSyncPaymentAsync(CurrentUserId, checkoutId, cancellationToken);
        return Ok(result);
    }
}
