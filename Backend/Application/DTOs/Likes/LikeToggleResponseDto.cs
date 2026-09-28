namespace Application.DTOs.Likes;

public class LikeToggleResponseDto
{
    public Guid PostId { get; set; }
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
}
