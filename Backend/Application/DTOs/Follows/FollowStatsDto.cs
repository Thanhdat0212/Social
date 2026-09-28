namespace Application.DTOs.Follows;

public class FollowStatsDto
{
    public Guid UserId { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowingByCurrentUser { get; set; }
}
