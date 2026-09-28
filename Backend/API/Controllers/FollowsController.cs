using Application.DTOs.Follows;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
public class FollowsController : ControllerBase
{
    private readonly IFollowService _followService;

    public FollowsController(IFollowService followService)
    {
        _followService = followService;
    }

    /// <summary>
    /// Theo dõi hoặc hủy theo dõi một người dùng (Toggle Follow)
    /// </summary>
    [HttpPost("{id:guid}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(FollowToggleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFollow(Guid id, CancellationToken cancellationToken)
    {
        var result = await _followService.ToggleFollowAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thống kê số lượng người theo dõi và đang theo dõi
    /// </summary>
    [HttpGet("{id:guid}/follow-stats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FollowStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowStats(Guid id, CancellationToken cancellationToken)
    {
        var stats = await _followService.GetUserFollowStatsAsync(id, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Lấy danh sách những người đang theo dõi người dùng này (Followers)
    /// </summary>
    [HttpGet("{id:guid}/followers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<UserFollowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowers(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var followers = await _followService.GetFollowersAsync(id, page, pageSize, cancellationToken);
        return Ok(followers);
    }

    /// <summary>
    /// Lấy danh sách những người mà người dùng này đang theo dõi (Following)
    /// </summary>
    [HttpGet("{id:guid}/following")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<UserFollowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowing(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var following = await _followService.GetFollowingAsync(id, page, pageSize, cancellationToken);
        return Ok(following);
    }
}
