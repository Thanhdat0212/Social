using Application.DTOs.Recommendation;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetRecentPostsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetCandidatePostsForFeedAsync(int limit = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetFollowingPostsAsync(IEnumerable<Guid> followingUserIds, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetPostsByInterestIdsAsync(IEnumerable<Guid> interestIds, int limit = 300, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetTrendingPostsAsync(TimeSpan timeWindow, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetExplorationPostsAsync(IEnumerable<Guid> excludeInterestIds, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetPostsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(Guid authorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetPostCountByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);

    // Tối ưu hóa Candidate Retrieval: Các phương thức truy vấn DTO siêu nhẹ (1 round-trip) phục vụ Pipeline gợi ý
    Task<IReadOnlyList<PostCandidateDto>> GetRecentCandidatesAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetFollowingCandidatesAsync(IEnumerable<Guid> followingUserIds, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetInterestCandidatesAsync(IEnumerable<Guid> interestIds, int limit = 300, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetTrendingCandidatesAsync(TimeSpan timeWindow, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetExplorationCandidatesAsync(IEnumerable<Guid> excludeInterestIds, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetAuthorRecentCandidatesAsync(Guid authorId, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCandidateDto>> GetCandidatesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
