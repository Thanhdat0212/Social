namespace Application.DTOs.Auth.Responses;

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
