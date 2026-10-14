using API.Filters;
using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.Interfaces.Marketplace.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/checkouts")]
[Authorize]
[EnableRateLimiting("checkout-policy")]
public class CheckoutsController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutsController(
        ICheckoutService checkoutService,
        ICurrentUserService currentUserService)
    {
        _checkoutService = checkoutService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Tạo phiên Checkout và khóa giữ mã kho đồng thời (FOR UPDATE SKIP LOCKED)
    /// Hỗ trợ Header 'Idempotency-Key' chống bấm đúp
    /// </summary>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(CheckoutDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKeyHeader) && string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            request.IdempotencyKey = idempotencyKeyHeader.Trim();
        }

        var result = await _checkoutService.CreateCheckoutAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetCheckoutById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lấy chi tiết phiên Checkout (kèm bộ đếm lùi thời gian hết hạn 15 phút)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CheckoutDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCheckoutById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _checkoutService.GetCheckoutByIdAsync(CurrentUserId, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy phiên thanh toán." });
        }
        return Ok(result);
    }
}
