using Application.DTOs.Posts;

namespace Application.Interfaces;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(CreatePostRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetRecentPostsAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default);
    Task<PostDto> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetMyPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetPostsByAuthorIdAsync(Guid authorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetPostCountByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);
}
