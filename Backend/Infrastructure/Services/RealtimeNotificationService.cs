using Application.DTOs.Comments;
using Application.DTOs.Posts;
using Application.DTOs.Realtime;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<SocialHub> _hubContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<SocialHub> hubContext,
        IUnitOfWork unitOfWork,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task PublishNewPostAsync(PostDto post, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Phát sóng tới toàn bộ người dùng đang online (để hiển thị Floating Pill trên Home Feed)
            await _hubContext.Clients.All.SendAsync("ReceiveNewPost", post, cancellationToken);
            _logger.LogInformation("Đã phát sóng bài viết mới {PostId} tới toàn bộ kết nối SocialHub.", post.Id);

            // 2. Gửi thông báo trực tiếp cho các follower của tác giả
            if (post.Author != null)
            {
                var followers = await _unitOfWork.UserFollows.GetFollowersAsync(post.Author.Id, 1, 100, cancellationToken);
                foreach (var follow in followers)
                {
                    var notification = new UserNotificationEventDto
                    {
                        Type = "new_post",
                        Message = $"{post.Author.DisplayName} vừa đăng một bài viết mới.",
                        TriggeredByUserId = post.Author.Id,
                        TriggeredByUserName = post.Author.DisplayName,
                        TriggeredByUserAvatar = post.Author.AvatarUrl,
                        TargetPostId = post.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    await _hubContext.Clients.Group($"user_{follow.FollowerId}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát sự kiện PublishNewPostAsync cho bài viết {PostId}", post.Id);
        }
    }

    public async Task PublishPostLikedAsync(Guid postId, int likeCount, Guid userId, bool isLiked, Guid postAuthorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var likeEvent = new PostLikeEventDto
            {
                PostId = postId,
                LikeCount = likeCount,
                UserId = userId,
                IsLiked = isLiked
            };

            // 1. Phát sóng tới toàn bộ người dùng để cập nhật số lượt thích trên bảng tin và bài viết
            await _hubContext.Clients.All
                .SendAsync("PostLiked", likeEvent, cancellationToken);

            // 2. Nếu là thả like và không phải tự like bài của mình, gửi notification cho chủ bài viết
            if (isLiked && userId != postAuthorId)
            {
                var actor = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                var actorName = !string.IsNullOrWhiteSpace(actor?.DisplayName) ? actor.DisplayName : "Một người dùng";
                var notification = new UserNotificationEventDto
                {
                    Type = "like",
                    Message = $"{actorName} đã thích bài viết của bạn.",
                    TriggeredByUserId = userId,
                    TriggeredByUserName = actorName,
                    TriggeredByUserAvatar = actor?.AvatarUrl,
                    TargetPostId = postId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát sự kiện PublishPostLikedAsync cho bài viết {PostId}", postId);
        }
    }

    public async Task PublishCommentAddedAsync(Guid postId, int totalCommentCount, CommentDto comment, Guid postAuthorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var commentEvent = new CommentAddedEventDto
            {
                PostId = postId,
                TotalCommentCount = totalCommentCount,
                Comment = comment
            };

            // 1. Phát sóng tới toàn bộ người dùng để cập nhật số lượng và danh sách bình luận bài viết
            await _hubContext.Clients.All
                .SendAsync("CommentAdded", commentEvent, cancellationToken);

            // 2. Gửi notification cho chủ bài viết nếu người comment không phải là chủ bài viết
            if (comment.Author != null && comment.Author.Id != postAuthorId)
            {
                var authorName = !string.IsNullOrWhiteSpace(comment.Author.DisplayName) ? comment.Author.DisplayName : "Một người dùng";
                var notification = new UserNotificationEventDto
                {
                    Type = "comment",
                    Message = $"{authorName} đã bình luận bài viết của bạn.",
                    TriggeredByUserId = comment.Author.Id,
                    TriggeredByUserName = authorName,
                    TriggeredByUserAvatar = comment.Author.AvatarUrl,
                    TargetPostId = postId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát sự kiện PublishCommentAddedAsync cho bài viết {PostId}", postId);
        }
    }

    public async Task SendNotificationToUserAsync(Guid userId, UserNotificationEventDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo tới User {UserId}", userId);
        }
    }

    public async Task PublishPostDeletedAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Phát sóng tới tất cả người dùng để loại bỏ bài viết khỏi feed
            await _hubContext.Clients.All.SendAsync("PostDeleted", postId.ToString(), cancellationToken);
            _logger.LogInformation("Đã phát sự kiện xóa bài viết {PostId} tới toàn bộ client.", postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát sự kiện PostDeleted cho bài viết {PostId}", postId);
        }
    }
}
