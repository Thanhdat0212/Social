using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Orders;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách đơn hàng đã mua của người dùng hiện tại (kèm phân trang, lọc theo trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BuyerOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetBuyerOrdersAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng đã mua của người dùng hiện tại (bao gồm các mã số đã được giải mã an toàn)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BuyerOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetBuyerOrderByIdAsync(CurrentUserId, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Sinh liên kết tải tệp tin số có chữ ký bảo mật và thời hạn hết hạn 30 phút (Buyer)
    /// </summary>
    [HttpGet("items/{orderItemId:guid}/download-url")]
    [ProducesResponseType(typeof(SignedDownloadUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignedDownloadUrl(
        Guid orderItemId,
        CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _orderService.GenerateSignedDownloadUrlAsync(CurrentUserId, orderItemId, baseUrl, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tải tệp tin số an toàn bằng liên kết có chữ ký HMAC-SHA256 (Hết hạn sau 30 phút)
    /// </summary>
    [HttpGet("~/api/marketplace/files/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFile(
        [FromQuery] Guid orderItemId,
        [FromQuery] Guid buyerUserId,
        [FromQuery] long expiresAt,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        var (storagePath, fileName, contentType) = await _orderService.ProcessFileDownloadAsync(
            orderItemId,
            buyerUserId,
            expiresAt,
            sig,
            cancellationToken);

        // Redirect 302 trực tiếp đến URL lưu trữ tệp tin trên Cloudinary / Storage
        return Redirect(storagePath);
    }
}
