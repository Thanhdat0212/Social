using Application.DTOs.Comments;

namespace Application.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateCommentAsync(Guid postId, CreateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetPostCommentsAsync(Guid postId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);
}
