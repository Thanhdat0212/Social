using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _adminProductService;
    private readonly ICurrentUserService _currentUserService;

    public AdminProductsController(
        IAdminProductService adminProductService,
        ICurrentUserService currentUserService)
    {
        _adminProductService = adminProductService;
        _currentUserService = currentUserService;
    }

    private Guid AdminId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Không xác định được danh tính Quản trị viên.");

    /// <summary>
    /// Quản trị viên xem danh sách toàn bộ sản phẩm trên hệ thống (lọc theo Shop, trạng thái hoặc từ khóa)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] Guid? shopId,
        [FromQuery] ProductStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminProductService.GetProductsAsync(shopId, status, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Quản trị viên gỡ bỏ sản phẩm vi phạm chính sách (Takedown)
    /// </summary>
    [HttpPost("{id:guid}/takedown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TakedownProduct(Guid id, [FromBody] TakedownProductRequest request, CancellationToken cancellationToken)
    {
        await _adminProductService.TakedownAsync(AdminId, id, request.Reason, cancellationToken);
        return Ok(new { message = "Đã gỡ bỏ sản phẩm vi phạm thành công." });
    }

    /// <summary>
    /// Quản trị viên dỡ bỏ lệnh khóa, cho phép sản phẩm hiển thị lại nếu đủ điều kiện
    /// </summary>
    [HttpDelete("{id:guid}/takedown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LiftTakedown(Guid id, CancellationToken cancellationToken)
    {
        await _adminProductService.LiftTakedownAsync(AdminId, id, cancellationToken);
        return Ok(new { message = "Đã dỡ bỏ lệnh khóa đối với sản phẩm thành công." });
    }
}
