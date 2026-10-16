using Application.DTOs.Marketplace.Shops;
using Application.Interfaces.Marketplace.Shops;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/shops")]
[Authorize(Roles = "Admin")]
public class AdminShopsController : ControllerBase
{
    private readonly IAdminShopService _adminShopService;

    public AdminShopsController(IAdminShopService adminShopService)
    {
        _adminShopService = adminShopService;
    }

    /// <summary>
    /// Lấy danh sách gian hàng (lọc theo trạng thái phê duyệt: Pending, Approved, Rejected)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShops(
        [FromQuery] ApprovalStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total, stats) = await _adminShopService.GetShopsAsync(status, search, page, pageSize, cancellationToken);
        return Ok(new
        {
            total,
            page,
            pageSize,
            items,
            stats
        });
    }

    /// <summary>
    /// Lấy chi tiết gian hàng (kèm snapshot nộp duyệt, toàn bộ lịch sử review và sanction)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminShopService.GetShopDetailAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Phê duyệt mở gian hàng
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveShop(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminShopService.ApproveShopAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Từ chối mở gian hàng (bắt buộc nhập lý do)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ShopDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectShop(Guid id, [FromBody] RejectShopRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminShopService.RejectShopAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Áp dụng chế tài / xử phạt gian hàng vi phạm (tạm ngưng hoạt động, cấm vĩnh viễn)
    /// </summary>
    [HttpPost("{id:guid}/sanctions")]
    [ProducesResponseType(typeof(ShopSanctionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddSanction(Guid id, [FromBody] CreateSanctionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminShopService.AddSanctionAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Gỡ bỏ chế tài xử phạt gian hàng
    /// </summary>
    [HttpDelete("{id:guid}/sanctions/{sanctionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LiftSanction(Guid id, Guid sanctionId, CancellationToken cancellationToken)
    {
        await _adminShopService.LiftSanctionAsync(id, sanctionId, cancellationToken);
        return Ok(new { message = "Đã gỡ bỏ quyết định xử phạt thành công." });
    }
}
