using Application.DTOs.Profile.Requests;
using Application.DTOs.Profile.Responses;
using Application.Interfaces;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IValidator<UpdateProfileRequestDto> _updateValidator;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

    public ProfileController(
        IProfileService profileService,
        IValidator<UpdateProfileRequestDto> updateValidator)
    {
        _profileService = profileService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lấy thông tin hồ sơ của người dùng hiện tại đang đăng nhập
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profileService.GetCurrentUserProfileAsync(cancellationToken);
            return Ok(profile);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật tên hiển thị và tiểu sử của người dùng
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.ToDictionary();
            return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors });
        }

        try
        {
            var profile = await _profileService.UpdateProfileAsync(request, cancellationToken);
            return Ok(profile);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tải lên ảnh đại diện lên Cloudinary (Multipart form-data, tối đa 5MB, định dạng JPG/PNG/WebP)
    /// </summary>
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AvatarUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp ảnh để tải lên." });
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            return BadRequest(new { message = "Kích thước ảnh đại diện không được vượt quá 5MB." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Định dạng file không hỗ trợ. Vui lòng chọn ảnh JPG, PNG hoặc WebP." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var avatarUrl = await _profileService.UploadAvatarAsync(stream, file.FileName, cancellationToken);

            return Ok(new AvatarUploadResponseDto { AvatarUrl = avatarUrl });
        }
        catch (ValidationAppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
