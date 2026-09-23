namespace Application.DTOs.Auth.Requests;

public class ResetPasswordRequestDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
