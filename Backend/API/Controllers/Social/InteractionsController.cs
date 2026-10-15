using Application.DTOs.Interactions;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionsController : ControllerBase
{
    private readonly IInteractionService _interactionService;

    public InteractionsController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    /// <summary>
    /// Ghi nhận 1 sự kiện tương tác của người dùng (View, Watch Time, Share, Save, Skip...)
    /// </summary>
    [HttpPost("track")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TrackInteraction(
        [FromBody] TrackInteractionRequestDto request,
        CancellationToken cancellationToken)
    {
        await _interactionService.TrackInteractionAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Ghi nhận hàng loạt sự kiện tương tác (Batch tracking từ Frontend khi lướt feed)
    /// </summary>
    [HttpPost("track-batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TrackBatchInteractions(
        [FromBody] BatchTrackInteractionsRequestDto request,
        CancellationToken cancellationToken)
    {
        await _interactionService.TrackBatchInteractionsAsync(request, cancellationToken);
        return NoContent();
    }
}
