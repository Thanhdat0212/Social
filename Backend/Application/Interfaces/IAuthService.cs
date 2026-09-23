using Application.DTOs.Auth.Requests;
using Application.DTOs.Auth.Responses;

namespace Application.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task ResendConfirmationAsync(ResendConfirmationRequestDto request, CancellationToken cancellationToken = default);

    Task<LoginResultDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<RefreshResultDto> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
