using Application.DTOs.Likes;

namespace Application.Interfaces;

public interface ILikeService
{
    Task<LikeToggleResponseDto> ToggleLikeAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<bool> IsPostLikedAsync(Guid postId, CancellationToken cancellationToken = default);
}
