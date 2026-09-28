using Application.DTOs.Interests;
using Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IInterestService _interestService;
    private readonly IValidator<SelectInterestsRequestDto> _selectValidator;

    public UsersController(
        IInterestService interestService,
        IValidator<SelectInterestsRequestDto> selectValidator)
    {
        _interestService = interestService;
        _selectValidator = selectValidator;
    }

    /// <summary>
    /// Lấy danh sách các chủ đề/sở thích mà người dùng hiện tại đã chọn
    /// </summary>
    [HttpGet("me/interests")]
    [ProducesResponseType(typeof(IReadOnlyList<InterestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInterests(CancellationToken cancellationToken)
    {
        var interests = await _interestService.GetMyInterestsAsync(cancellationToken);
        return Ok(interests);
    }

    /// <summary>
    /// Chọn hoặc cập nhật danh sách sở thích ban đầu (Onboarding - tối thiểu 3 chủ đề)
    /// </summary>
    [HttpPost("me/interests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SelectInterests(
        [FromBody] SelectInterestsRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _selectValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await _interestService.SelectMyInterestsAsync(request, cancellationToken);
        return Ok(new { message = "Lưu danh sách sở thích thành công." });
    }

    /// <summary>
    /// Lấy hồ sơ trọng số sở thích thuật toán của người dùng hiện tại
    /// </summary>
    [HttpGet("me/preferences")]
    [ProducesResponseType(typeof(IReadOnlyList<UserPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPreferences(CancellationToken cancellationToken)
    {
        var preferences = await _interestService.GetMyPreferencesAsync(cancellationToken);
        return Ok(preferences);
    }

    /// <summary>
    /// Kiểm tra trạng thái Onboarding của người dùng (đã chọn tối thiểu 3 sở thích hay chưa)
    /// </summary>
    [HttpGet("me/onboarding-status")]
    [ProducesResponseType(typeof(OnboardingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOnboardingStatus(CancellationToken cancellationToken)
    {
        var status = await _interestService.GetMyOnboardingStatusAsync(cancellationToken);
        return Ok(status);
    }
}
