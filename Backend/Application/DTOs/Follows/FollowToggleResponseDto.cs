namespace Application.DTOs.Follows;

public class FollowToggleResponseDto
{
    public Guid TargetUserId { get; set; }
    public bool IsFollowing { get; set; }
    public int FollowersCount { get; set; }
}
