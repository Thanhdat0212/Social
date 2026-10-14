using Application.Common;
using Application.DTOs.Marketplace.Complaints;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Complaints;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/complaints")]
[Authorize(Roles = "Admin")]
public class AdminComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ICurrentUserService _currentUserService;

    public AdminComplaintsController(
        IComplaintService complaintService,
        ICurrentUserService currentUserService)
    {
        _complaintService = complaintService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Quản trị viên chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách hàng đợi khiếu nại cần Admin phân xử (bị Shop từ chối hoặc quá 24h shop không phản hồi)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplaintListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminComplaints(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await _complaintService.GetAdminComplaintsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin đưa ra phán quyết khiếu nại (Hoàn tiền, Đổi mã mới, hoặc Từ chối khiếu nại)
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveComplaint(
        Guid id,
        [FromBody] AdminResolveComplaintRequest request,
        CancellationToken cancellationToken)
    {
        await _complaintService.ResolveComplaintAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(new { message = "Đã ban hành phán quyết khiếu nại thành công." });
    }
}
