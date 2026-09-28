using Application.DTOs.Follows;

namespace Application.Interfaces;

public interface IFollowService
{
    Task<FollowToggleResponseDto> ToggleFollowAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<FollowStatsDto> GetUserFollowStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserFollowDto>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserFollowDto>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
