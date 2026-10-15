using Application.DTOs.Posts;

namespace Application.Interfaces;

public interface IFeedService
{
    /// <summary>
    /// Bảng tin gợi ý thông minh (For You Feed) dựa trên thuật toán 70% Cá nhân hoá, 20% Mở rộng, 10% Khám phá mới
    /// </summary>
    Task<IReadOnlyList<PostDto>> GetForYouFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bảng tin từ những người dùng mà tài khoản hiện tại đang theo dõi (Following Feed)
    /// </summary>
    Task<IReadOnlyList<PostDto>> GetFollowingFeedAsync(int page = 1, int pageSize = 20, IEnumerable<Guid>? seenPostIds = null, CancellationToken cancellationToken = default);
}
