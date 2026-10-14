using API.Filters;
using Application.Common;
using Application.DTOs.Marketplace.Payouts;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Payouts;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Authorize]
public class ShopPayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly ICurrentUserService _currentUserService;

    public ShopPayoutsController(
        IPayoutService payoutService,
        ICurrentUserService currentUserService)
    {
        _payoutService = payoutService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Cài đặt hoặc cập nhật tài khoản ngân hàng nhận tiền của Gian hàng (Mã hóa AES-GCM, khóa rút tiền 24h)
    /// </summary>
    [HttpPost("api/shop/bank-account")]
    [ProducesResponseType(typeof(ShopBankAccountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetBankAccount([FromBody] CreateShopBankAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _payoutService.SetBankAccountAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem thông tin tài khoản ngân hàng đang liên kết của Gian hàng
    /// </summary>
    [HttpGet("api/shop/bank-account")]
    [ProducesResponseType(typeof(ShopBankAccountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBankAccount(CancellationToken cancellationToken)
    {
        var result = await _payoutService.GetBankAccountAsync(CurrentUserId, cancellationToken);
        if (result == null) return NotFound(new { message = "Gian hàng chưa cài đặt tài khoản ngân hàng nhận tiền." });
        return Ok(result);
    }

    /// <summary>
    /// Yêu cầu rút tiền từ số dư khả dụng về tài khoản ngân hàng (Tối thiểu 100.000 VNĐ, chống duplicate submit)
    /// </summary>
    [HttpPost("api/shop/payouts")]
    [Idempotent]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPayout([FromBody] CreatePayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _payoutService.RequestPayoutAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách lịch sử các yêu cầu rút tiền của Gian hàng
    /// </summary>
    [HttpGet("api/shop/payouts")]
    [ProducesResponseType(typeof(PagedResult<PayoutDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] PayoutStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await _payoutService.GetShopPayoutsAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một lệnh rút tiền
    /// </summary>
    [HttpGet("api/shop/payouts/{id:guid}")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayoutDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _payoutService.GetShopPayoutDetailAsync(CurrentUserId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Chủ gian hàng tự hủy lệnh rút tiền khi lệnh còn đang ở trạng thái chờ duyệt (Requested)
    /// </summary>
    [HttpPost("api/shop/payouts/{id:guid}/cancel")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelPayout(
        Guid id,
        [FromBody] ShopCancelPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payoutService.CancelPayoutAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }
}
