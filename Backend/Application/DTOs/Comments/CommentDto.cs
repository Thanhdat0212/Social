namespace Application.DTOs.Comments;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public CommentAuthorDto Author { get; set; } = null!;
    public List<CommentDto> Replies { get; set; } = new();
}
