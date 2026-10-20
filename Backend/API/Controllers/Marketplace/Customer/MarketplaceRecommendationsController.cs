using Application.Common;
using Application.DTOs.Marketplace.Recommendations;
using Application.Interfaces.Marketplace.Recommendations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace.Customer;

/// <summary>
/// API Cung cấp các luồng đề xuất sản phẩm cá nhân hóa cho Marketplace
/// Hỗ trợ cả người dùng đã đăng nhập (Personalized) và khách vãng lai (Cold-start / Session-based)
/// </summary>
[ApiController]
[Route("api/marketplace/recommendations")]
[AllowAnonymous]
public class MarketplaceRecommendationsController : ControllerBase
{
    private readonly IMarketplaceRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUserService;

    public MarketplaceRecommendationsController(
        IMarketplaceRecommendationService recommendationService,
        ICurrentUserService currentUserService)
    {
        _recommendationService = recommendationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm đề xuất "Dành riêng cho bạn" trên trang chủ Marketplace
    /// Kết hợp đa luồng: Sở thích mạng xã hội, Lịch sử tương tác, Xu hướng và Đa dạng hóa danh mục
    /// </summary>
    /// <param name="limit">Số lượng sản phẩm đề xuất (mặc định 12, tối đa 50)</param>
    /// <param name="sessionId">Mã phiên duyệt web của khách vãng lai (truyền qua Header hoặc Query)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("for-you")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForYou(
        [FromQuery] int limit = 12,
        [FromHeader(Name = "X-Session-Id")] string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var userId = _currentUserService.UserId;

        var recommendations = await _recommendationService.GetForYouProductsAsync(
            userId,
            sessionId,
            safeLimit,
            cancellationToken);

        return Ok(recommendations);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm tương tự (Alternative / Content-based) trên trang chi tiết sản phẩm
    /// </summary>
    /// <param name="productId">ID sản phẩm đang xem</param>
    /// <param name="limit">Số lượng đề xuất (mặc định 8, tối đa 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId:guid}/related")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelated(
        Guid productId,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 20);
        var recommendations = await _recommendationService.GetRelatedProductsAsync(
            productId,
            safeLimit,
            cancellationToken);

        return Ok(recommendations);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm thường được mua cùng (Frequently Bought Together)
    /// Dựa trên ma trận đồng xuất hiện trong đơn hàng (Jaccard Index)
    /// </summary>
    /// <param name="productId">ID sản phẩm đang xem</param>
    /// <param name="limit">Số lượng gợi ý mua kèm (mặc định 4, tối đa 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId:guid}/bought-together")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoughtTogether(
        Guid productId,
        [FromQuery] int limit = 4,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 10);
        var recommendations = await _recommendationService.GetFrequentlyBoughtTogetherAsync(
            productId,
            safeLimit,
            cancellationToken);

        return Ok(recommendations);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm thịnh hành (Trending / Top Bán chạy / Đánh giá cao)
    /// </summary>
    /// <param name="categoryId">ID danh mục (tùy chọn để xem trending theo từng ngành hàng)</param>
    /// <param name="limit">Số lượng sản phẩm (mặc định 8, tối đa 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("trending")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationProductCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrending(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 24);
        var recommendations = await _recommendationService.GetTrendingProductsAsync(
            categoryId,
            safeLimit,
            cancellationToken);

        return Ok(recommendations);
    }
}
