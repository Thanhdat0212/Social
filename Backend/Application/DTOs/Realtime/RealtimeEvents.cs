using Application.DTOs.Comments;
using Application.DTOs.Posts;

namespace Application.DTOs.Realtime;

public class NewPostEventDto
{
    public PostDto Post { get; set; } = null!;
}

public class PostLikeEventDto
{
    public Guid PostId { get; set; }
    public int LikeCount { get; set; }
    public Guid UserId { get; set; }
    public bool IsLiked { get; set; }
}

public class CommentAddedEventDto
{
    public Guid PostId { get; set; }
    public int TotalCommentCount { get; set; }
    public CommentDto Comment { get; set; } = null!;
}

public class UserNotificationEventDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty; // "like", "comment", "new_post", "follow"
    public string Message { get; set; } = string.Empty;
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByUserName { get; set; }
    public string? TriggeredByUserAvatar { get; set; }
    public Guid? TargetPostId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
