using API.Auth;
using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<ResendConfirmationRequest> _resendValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<ResendConfirmationRequest> resendValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _resendValidator = resendValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>
    /// Đăng ký tài khoản mới và gửi email xác minh
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.ToDictionary();
            return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors });
        }

        try
        {
            await _authService.RegisterAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { message = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản." });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xác minh email bằng token trong link
    /// </summary>
    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Thông tin xác minh không hợp lệ." });
        }

        try
        {
            await _authService.ConfirmEmailAsync(userId, token, cancellationToken);
            return Ok(new { message = "Xác minh email thành công. Bây giờ bạn có thể đăng nhập." });
        }
        catch (ValidationAppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gửi lại email xác minh (luôn trả về 200 kèm cooldown 60s)
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _resendValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.ToDictionary();
            return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors });
        }

        await _authService.ResendConfirmationAsync(request, cancellationToken);
        return Ok(new { message = "Nếu email tồn tại và chưa xác minh, liên kết xác minh mới đã được gửi." });
    }

    /// <summary>
    /// Đăng nhập bằng Email và Password. Trả về AccessToken trong Body và gán RefreshToken vào Cookie httpOnly
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.ToDictionary();
            return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ipAddress, cancellationToken);

            // Gán cookie httpOnly cho refresh token
            RefreshTokenCookie.Append(Response, result.RawRefreshToken, result.RefreshTokenExpiresAt);

            return Ok(new LoginResponse
            {
                AccessToken = result.AccessToken,
                ExpiresAt = result.ExpiresAt,
                User = result.User
            });
        }
        catch (ValidationAppException ex) when (ex.Message.Contains("EMAIL_NOT_CONFIRMED") || ex.Errors.ContainsKey("EMAIL_NOT_CONFIRMED"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "EMAIL_NOT_CONFIRMED", message = ex.Message });
        }
        catch (ValidationAppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Làm mới Access Token thông qua Refresh Token lưu trong Cookie httpOnly
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var rawRefreshToken = RefreshTokenCookie.Get(Request);
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized(new { message = "Không tìm thấy refresh token trong cookie." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshAsync(rawRefreshToken, ipAddress, cancellationToken);

            // Cập nhật cookie với token xoay vòng mới
            RefreshTokenCookie.Append(Response, result.RawRefreshToken, result.RefreshTokenExpiresAt);

            return Ok(new RefreshResponse
            {
                AccessToken = result.AccessToken,
                ExpiresAt = result.ExpiresAt
            });
        }
        catch (ValidationAppException ex)
        {
            // Xoá cookie nếu refresh token không hợp lệ
            RefreshTokenCookie.Delete(Response);
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Đăng xuất: Thu hồi refresh token trong DB và xoá cookie httpOnly
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawRefreshToken = RefreshTokenCookie.Get(Request);
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _authService.LogoutAsync(rawRefreshToken, cancellationToken);
        }

        // Luôn xoá cookie phía client
        RefreshTokenCookie.Delete(Response);

        return NoContent();
    }
}
