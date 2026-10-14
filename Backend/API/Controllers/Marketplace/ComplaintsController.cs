using Application.Common;
using Application.DTOs.Marketplace.Complaints;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Complaints;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ICurrentUserService _currentUserService;

    public ComplaintsController(
        IComplaintService complaintService,
        ICurrentUserService currentUserService)
    {
        _complaintService = complaintService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Gửi khiếu nại cho sản phẩm số trong đơn hàng (kèm bằng chứng ảnh/video trong vòng 48h)
    /// </summary>
    [HttpPost("~/api/orders/{orderId:guid}/items/{orderItemId:guid}/complaints")]
    [ProducesResponseType(typeof(ComplaintDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateComplaint(
        Guid orderId,
        Guid orderItemId,
        [FromBody] CreateComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _complaintService.CreateComplaintAsync(CurrentUserId, orderId, orderItemId, request, cancellationToken);
        return CreatedAtAction(nameof(GetComplaintById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lấy danh sách khiếu nại của tôi (người mua)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplaintListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyComplaints(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _complaintService.GetBuyerComplaintsAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một khiếu nại của người mua
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ComplaintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComplaintById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _complaintService.GetBuyerComplaintByIdAsync(CurrentUserId, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy khiếu nại hoặc bạn không có quyền xem." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Người mua tự hủy khiếu nại (khi chưa được Shop/Admin xử lý)
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelComplaint(Guid id, CancellationToken cancellationToken)
    {
        await _complaintService.CancelComplaintAsync(CurrentUserId, id, cancellationToken);
        return Ok(new { message = "Đã hủy khiếu nại thành công." });
    }
}
