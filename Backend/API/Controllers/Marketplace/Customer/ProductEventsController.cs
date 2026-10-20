using Application.Common;
using Application.DTOs.Marketplace.Recommendations;
using Application.Interfaces.Marketplace.Recommendations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace.Customer;

/// <summary>
/// API Thu thập hành vi người dùng (Tracking Ingestion) và Quản lý lịch sử tìm kiếm
/// Tối ưu hóa hiệu năng bằng cách đẩy vào RAM Channel không khóa (Non-blocking)
/// </summary>
[ApiController]
[Route("api/marketplace")]
public class ProductEventsController : ControllerBase
{
    private readonly IProductEventChannel _eventChannel;
    private readonly IMarketplaceRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUserService;

    public ProductEventsController(
        IProductEventChannel eventChannel,
        IMarketplaceRecommendationService recommendationService,
        ICurrentUserService currentUserService)
    {
        _eventChannel = eventChannel;
        _recommendationService = recommendationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Ghi nhận hành vi tương tác sản phẩm (View, DwellTime, Click, AddToCart, Wishlist, Share)
    /// Đẩy sự kiện vào In-Memory Channel và trả về 202 Accepted ngay lập tức mà không block request
    /// </summary>
    /// <param name="dto">Thông tin hành vi tương tác</param>
    [HttpPost("events")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackProductEvent(
        [FromBody] ProductEventCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.ProductId == Guid.Empty)
        {
            return BadRequest(new { message = "ProductId không hợp lệ." });
        }

        var userId = _currentUserService.UserId;

        var message = new ProductEventMessage
        {
            UserId = userId,
            SessionId = dto.SessionId,
            ProductId = dto.ProductId,
            EventType = dto.EventType,
            DwellTimeSeconds = dto.DwellTimeSeconds,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Đẩy vào In-Memory Channel (thao tác RAM non-blocking < 0.1ms)
        await _eventChannel.EnqueueEventAsync(message, cancellationToken);

        return Accepted(new { message = "Sự kiện tương tác đã được tiếp nhận." });
    }

    /// <summary>
    /// Lưu từ khóa tìm kiếm khi người dùng gõ tìm sản phẩm
    /// </summary>
    [HttpPost("search-history")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordSearch(
        [FromBody] RecordSearchQueryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        await _recommendationService.RecordSearchQueryAsync(userId, request, cancellationToken);
        return Ok(new { message = "Đã lưu lịch sử tìm kiếm." });
    }

    /// <summary>
    /// Lấy danh sách từ khóa tìm kiếm gần đây của người dùng hiện tại
    /// </summary>
    [HttpGet("search-history/recent")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<SearchHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentSearches(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var searches = await _recommendationService.GetRecentSearchesAsync(userId.Value, limit, cancellationToken);
        return Ok(searches);
    }
}
