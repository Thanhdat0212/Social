namespace Application.DTOs.Profile.Requests;

public class UpdateProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
}
