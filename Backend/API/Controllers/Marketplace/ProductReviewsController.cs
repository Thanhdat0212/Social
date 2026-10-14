using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/products/{productId:guid}/reviews")]
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;

    public ProductReviewsController(
        IProductReviewService reviewService,
        ICurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách đánh giá của một sản phẩm (Công khai, có phân trang)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetReviewsByProductIdAsync(productId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thống kê số sao trung bình và biểu đồ phân bổ sao của sản phẩm (Công khai)
    /// </summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductReviewSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewSummary(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _reviewService.GetReviewSummaryByProductIdAsync(productId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gửi đánh giá cho món hàng trong đơn đã thanh toán & giao nhận thành công (Buyer)
    /// </summary>
    [HttpPost("~/api/orders/items/{orderItemId:guid}/reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ProductReviewDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReview(
        Guid orderItemId,
        [FromBody] CreateProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        request.OrderItemId = orderItemId;
        var result = await _reviewService.CreateReviewAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Kiểm tra xem món hàng này đã được đánh giá chưa (Buyer)
    /// </summary>
    [HttpGet("~/api/orders/items/{orderItemId:guid}/reviewed")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckHasReviewed(
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        var hasReviewed = await _reviewService.HasReviewedOrderItemAsync(orderItemId, cancellationToken);
        return Ok(new { hasReviewed });
    }

    /// <summary>
    /// Chủ gian hàng phản hồi đánh giá của người mua (Seller)
    /// </summary>
    [HttpPost("~/api/shop/reviews/{reviewId:guid}/reply")]
    [Authorize]
    [ProducesResponseType(typeof(ProductReviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplyReview(
        Guid reviewId,
        [FromBody] SellerReplyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.SellerReplyAsync(CurrentUserId, reviewId, request, cancellationToken);
        return Ok(result);
    }
}
