using Application.Common;
using Application.DTOs.Marketplace.Inventory;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Inventory;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/variants/{variantId:guid}")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public InventoryController(
        IInventoryService inventoryService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Nạp mã số / tài khoản số hàng loạt vào kho của biến thể (Mỗi dòng một mã, tối đa 1000 dòng)
    /// </summary>
    [HttpPost("codes/bulk")]
    [ProducesResponseType(typeof(BulkImportCodesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkImportCodes(
        Guid variantId,
        [FromBody] BulkImportCodesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.BulkImportCodesAsync(CurrentUserId, variantId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem danh sách mã trong kho của biến thể (mã được che bảo mật ***-XXXX)
    /// </summary>
    [HttpGet("codes")]
    [ProducesResponseType(typeof(PagedResult<DigitalCodeItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCodes(
        Guid variantId,
        [FromQuery] DigitalCodeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetCodesAsync(CurrentUserId, variantId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem bảng thống kê tồn kho theo các trạng thái (Sẵn sàng bán, Đang giữ chỗ, Đã giao, Bị lỗi)
    /// </summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(InventorySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventorySummary(
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventorySummaryAsync(CurrentUserId, variantId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu một mã trong kho bị lỗi / hỏng (Invalid)
    /// </summary>
    [HttpPost("codes/{codeId:guid}/invalidate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateCode(
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        await _inventoryService.InvalidateCodeAsync(CurrentUserId, variantId, codeId, cancellationToken);
        return Ok(new { message = "Đã đánh dấu mã số bị lỗi (Invalid) thành công." });
    }

    /// <summary>
    /// Xóa một mã khỏi kho (chỉ áp dụng với mã còn ở trạng thái Available)
    /// </summary>
    [HttpDelete("codes/{codeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCode(
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        await _inventoryService.DeleteCodeAsync(CurrentUserId, variantId, codeId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cấu hình tệp tin số cho biến thể loại File (source code, tài liệu e-book)
    /// </summary>
    [HttpPost("file")]
    [ProducesResponseType(typeof(DigitalFileDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfigureDigitalFile(
        Guid variantId,
        [FromBody] ConfigureDigitalFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.ConfigureDigitalFileAsync(CurrentUserId, variantId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin tệp tin số gắn liền với biến thể
    /// </summary>
    [HttpGet("file")]
    [ProducesResponseType(typeof(DigitalFileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDigitalFile(
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetDigitalFileAsync(CurrentUserId, variantId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Biến thể này chưa được cấu hình tệp tin số." });
        }
        return Ok(result);
    }
}
