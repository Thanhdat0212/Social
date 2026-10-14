using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Orders;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/orders")]
[Authorize]
public class ShopOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public ShopOrdersController(
        IOrderService orderService,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách đơn hàng thuộc Shop của người bán hiện tại (kèm phân trang, lọc theo trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SellerOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetShopOrdersAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng của Shop (chỉ hiển thị số lượng và mặt hàng, không lộ mã đã giao)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SellerOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetShopOrderByIdAsync(CurrentUserId, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng của shop hoặc bạn không phải chủ sở hữu." });
        }

        return Ok(result);
    }
}
