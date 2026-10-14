using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.Interfaces.Marketplace.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;

    public CartController(
        ICartService cartService,
        ICurrentUserService currentUserService)
    {
        _cartService = cartService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy thông tin giỏ hàng của người dùng hiện tại (kèm giá động thời gian thực và trạng thái khả dụng)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await _cartService.GetCartAsync(CurrentUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        var result = await _cartService.AddToCartAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật số lượng của một món hàng trong giỏ
    /// </summary>
    [HttpPatch("items/{itemId:guid}")]
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItemQuantity(Guid itemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _cartService.UpdateItemQuantityAsync(CurrentUserId, itemId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa một món hàng khỏi giỏ
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _cartService.RemoveItemAsync(CurrentUserId, itemId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(CurrentUserId, cancellationToken);
        return NoContent();
    }
}
