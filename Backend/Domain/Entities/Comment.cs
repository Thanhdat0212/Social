using Domain.Common;

namespace Domain.Entities;

public class Comment : BaseEntity
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }

    public string Content { get; set; } = string.Empty;

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
