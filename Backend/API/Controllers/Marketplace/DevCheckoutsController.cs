using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.Interfaces.Marketplace.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/dev/checkouts")]
[Authorize]
public class DevCheckoutsController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public DevCheckoutsController(
        ICheckoutService checkoutService,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _checkoutService = checkoutService;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Giả lập thanh toán thành công / thất bại (CHỈ KHẢ DỤNG TRONG MÔI TRƯỜNG DEVELOPMENT)
    /// </summary>
    [HttpPost("{id:guid}/mock-pay")]
    [ProducesResponseType(typeof(MockPayResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MockPay(
        Guid id,
        [FromBody] MockPayRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Mock payment chỉ được phép sử dụng trong môi trường Development." });
        }

        var result = await _checkoutService.MockPayAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }
}
