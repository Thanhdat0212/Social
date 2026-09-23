namespace Application.DTOs.Profile.Responses;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
