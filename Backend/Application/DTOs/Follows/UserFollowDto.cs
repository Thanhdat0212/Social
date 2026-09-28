namespace Application.DTOs.Follows;

public class UserFollowDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime FollowedAtUtc { get; set; }
}
