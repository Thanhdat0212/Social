using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = new();
    public PostStatus Status { get; set; } = PostStatus.Published;

    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public int ViewCount { get; set; } = 0;

    // Navigation properties
    public ICollection<PostInterest> PostInterests { get; set; } = new List<PostInterest>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}
