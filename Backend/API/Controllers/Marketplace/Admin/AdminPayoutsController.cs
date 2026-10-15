using Application.Common;
using Application.DTOs.Marketplace.Payouts;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Payouts;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/payouts")]
[Authorize(Roles = "Admin")]
public class AdminPayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly ICurrentUserService _currentUserService;

    public AdminPayoutsController(
        IPayoutService payoutService,
        ICurrentUserService currentUserService)
    {
        _payoutService = payoutService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Quản trị viên chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách hàng đợi yêu cầu rút tiền của các Gian hàng trên toàn hệ thống
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PayoutDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] PayoutStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await _payoutService.GetAdminPayoutsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một lệnh rút tiền kèm lịch sử trạng thái
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayoutDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _payoutService.GetAdminPayoutDetailAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin phê duyệt lệnh rút tiền (Chuyển sang Approved)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePayout(
        Guid id,
        [FromBody] AdminApprovePayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payoutService.ApprovePayoutAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin từ chối lệnh rút tiền (Bút toán đảo hoàn tiền khả dụng về lại ví gian hàng)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectPayout(
        Guid id,
        [FromBody] AdminRejectPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payoutService.RejectPayoutAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin xác nhận đã chuyển khoản ngân hàng thành công (Giải phóng PayoutPending sang BankClearing)
    /// </summary>
    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPaidPayout(
        Guid id,
        [FromBody] AdminMarkPaidPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payoutService.MarkPaidPayoutAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin đánh dấu lệnh rút tiền bị timeout / kết quả chưa rõ từ ngân hàng (Chờ tra soát, không tự động hoàn tiền)
    /// </summary>
    [HttpPost("{id:guid}/mark-unknown")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkUnknownPayout(
        Guid id,
        [FromBody] AdminMarkUnknownPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payoutService.MarkUnknownPayoutAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }
}
