using API.Auth;
using Application.DTOs.Auth.Requests;
using Application.DTOs.Auth.Responses;
using Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<ResendConfirmationRequestDto> _resendValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<ResendConfirmationRequestDto> resendValidator,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _resendValidator = resendValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    /// <summary>
    /// Đăng ký tài khoản mới và gửi email xác minh
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { message = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản." });
    }

    /// <summary>
    /// Xác minh email bằng token trong link
    /// </summary>
    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Lỗi dữ liệu yêu cầu",
                Detail = "Thông tin xác minh không hợp lệ."
            });
        }

        await _authService.ConfirmEmailAsync(userId, token, cancellationToken);
        return Ok(new { message = "Xác minh email thành công. Bây giờ bạn có thể đăng nhập." });
    }

    /// <summary>
    /// Gửi lại email xác minh (luôn trả về 200 kèm cooldown 60s)
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _resendValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await _authService.ResendConfirmationAsync(request, cancellationToken);
        return Ok(new { message = "Nếu email tồn tại và chưa xác minh, liên kết xác minh mới đã được gửi." });
    }

    /// <summary>
    /// Đăng nhập bằng Email và Password. Trả về AccessToken trong Body và gán RefreshToken vào Cookie httpOnly
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        // Gán cookie httpOnly cho refresh token
        RefreshTokenCookie.Append(Response, result.RawRefreshToken, result.RefreshTokenExpiresAt);

        return Ok(new LoginResponseDto
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt,
            User = result.User
        });
    }

    /// <summary>
    /// Làm mới Access Token thông qua Refresh Token lưu trong Cookie httpOnly
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var rawRefreshToken = RefreshTokenCookie.Get(Request);
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Không có quyền truy cập",
                Detail = "Không tìm thấy refresh token trong cookie."
            });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshAsync(rawRefreshToken, ipAddress, cancellationToken);

            // Cập nhật cookie với token xoay vòng mới
            RefreshTokenCookie.Append(Response, result.RawRefreshToken, result.RefreshTokenExpiresAt);

            return Ok(new RefreshResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresAt = result.ExpiresAt
            });
        }
        catch
        {
            // Xoá cookie nếu refresh token không hợp lệ và ném exception cho GlobalExceptionHandler xử lý
            RefreshTokenCookie.Delete(Response);
            throw;
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

    /// <summary>
    /// Yêu cầu đặt lại mật khẩu: Gửi email chứa liên kết reset mật khẩu 1 giờ (luôn trả 200)
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(new { message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi đến hộp thư của bạn." });
    }

    /// <summary>
    /// Đặt lại mật khẩu mới bằng token trong link email, đồng thời thu hồi toàn bộ phiên đăng nhập
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới." });
    }
}
