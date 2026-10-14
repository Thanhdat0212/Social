namespace Application.DTOs.Auth.Responses;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "Active";
}
