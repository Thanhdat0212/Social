using Application.Common;
using Application.DTOs.Marketplace.Complaints;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Complaints;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/complaints")]
[Authorize]
public class ShopComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ICurrentUserService _currentUserService;

    public ShopComplaintsController(
        IComplaintService complaintService,
        ICurrentUserService currentUserService)
    {
        _complaintService = complaintService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách các khiếu nại gửi tới gian hàng của tôi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplaintListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopComplaints(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _complaintService.GetShopComplaintsAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Chủ Shop phản hồi khiếu nại (Chấp nhận hoàn tiền, Đổi mã mới, hoặc Từ chối kèm lý do trong 24h)
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RespondComplaint(
        Guid id,
        [FromBody] ShopRespondComplaintRequest request,
        CancellationToken cancellationToken)
    {
        await _complaintService.RespondComplaintAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(new { message = "Đã gửi phản hồi khiếu nại thành công." });
    }
}
