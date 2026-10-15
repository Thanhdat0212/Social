using System.Text.Json;
using Application.DTOs.Comments;
using Application.DTOs.Notifications;
using Application.DTOs.Posts;
using Application.DTOs.Realtime;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
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

            // 2. Gửi thông báo trực tiếp và lưu database cho các follower của tác giả
            if (post.Author != null)
            {
                var followers = await _unitOfWork.UserFollows.GetFollowersAsync(post.Author.Id, 1, 100, cancellationToken);
                foreach (var follow in followers)
                {
                    var dataJson = JsonSerializer.Serialize(new
                    {
                        targetPostId = post.Id,
                        actorId = post.Author.Id,
                        actorName = post.Author.DisplayName,
                        actorAvatar = post.Author.AvatarUrl
                    });

                    var entity = new Notification
                    {
                        UserId = follow.FollowerId,
                        Type = "new_post",
                        Title = "Bài viết mới",
                        Body = $"{post.Author.DisplayName} vừa đăng một bài viết mới.",
                        DataJson = dataJson,
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    await _unitOfWork.Notifications.AddAsync(entity, cancellationToken);

                    var notification = new UserNotificationEventDto
                    {
                        Id = entity.Id,
                        Type = "new_post",
                        Message = entity.Body,
                        TriggeredByUserId = post.Author.Id,
                        TriggeredByUserName = post.Author.DisplayName,
                        TriggeredByUserAvatar = post.Author.AvatarUrl,
                        TargetPostId = post.Id,
                        CreatedAtUtc = entity.CreatedAtUtc
                    };

                    await _hubContext.Clients.Group($"user_{follow.FollowerId}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);

                    var persistentDto = new NotificationDto
                    {
                        Id = entity.Id,
                        Type = entity.Type,
                        Title = entity.Title,
                        Body = entity.Body,
                        IsRead = entity.IsRead,
                        DataJson = entity.DataJson,
                        CreatedAtUtc = entity.CreatedAtUtc
                    };

                    await _hubContext.Clients.Group($"user_{follow.FollowerId}")
                        .SendAsync("ReceivePersistentNotification", persistentDto, cancellationToken);
                }

                if (followers.Count > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
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

            // 2. Nếu là thả like và không phải tự like bài của mình, lưu database và gửi notification cho chủ bài viết
            if (isLiked && userId != postAuthorId)
            {
                var actor = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                var actorName = !string.IsNullOrWhiteSpace(actor?.DisplayName) ? actor.DisplayName : "Một người dùng";
                var dataJson = JsonSerializer.Serialize(new
                {
                    targetPostId = postId,
                    actorId = userId,
                    actorName = actorName,
                    actorAvatar = actor?.AvatarUrl
                });

                var entity = new Notification
                {
                    UserId = postAuthorId,
                    Type = "like",
                    Title = "Lượt thích mới",
                    Body = $"{actorName} đã thích bài viết của bạn.",
                    DataJson = dataJson,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var notification = new UserNotificationEventDto
                {
                    Id = entity.Id,
                    Type = "like",
                    Message = entity.Body,
                    TriggeredByUserId = userId,
                    TriggeredByUserName = actorName,
                    TriggeredByUserAvatar = actor?.AvatarUrl,
                    TargetPostId = postId,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);

                var persistentDto = new NotificationDto
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    Title = entity.Title,
                    Body = entity.Body,
                    IsRead = entity.IsRead,
                    DataJson = entity.DataJson,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceivePersistentNotification", persistentDto, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát sự kiện PublishPostLikedAsync cho bài viết {PostId}", postId);
        }
    }

    public async Task PublishCommentAddedAsync(Guid postId, int totalCommentCount, CommentDto comment, Guid postAuthorId, Guid? parentAuthorId = null, CancellationToken cancellationToken = default)
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

            var authorName = !string.IsNullOrWhiteSpace(comment.Author?.DisplayName) ? comment.Author.DisplayName : "Một người dùng";
            var commentSnippet = comment.Content?.Length > 60 ? comment.Content.Substring(0, 57) + "..." : comment.Content;

            // 2. Gửi notification và lưu database cho chủ bài viết nếu người comment không phải là chủ bài viết
            if (comment.Author != null && comment.Author.Id != postAuthorId)
            {
                var dataJson = JsonSerializer.Serialize(new
                {
                    targetPostId = postId,
                    commentId = comment.Id,
                    actorId = comment.Author.Id,
                    actorName = authorName,
                    actorAvatar = comment.Author.AvatarUrl
                });

                var bodyText = !string.IsNullOrWhiteSpace(commentSnippet)
                    ? $"{authorName} đã bình luận bài viết của bạn: \"{commentSnippet}\""
                    : $"{authorName} đã bình luận bài viết của bạn.";

                var entity = new Notification
                {
                    UserId = postAuthorId,
                    Type = "comment",
                    Title = "Bình luận mới",
                    Body = bodyText,
                    DataJson = dataJson,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var notification = new UserNotificationEventDto
                {
                    Id = entity.Id,
                    Type = "comment",
                    Message = entity.Body,
                    TriggeredByUserId = comment.Author.Id,
                    TriggeredByUserName = authorName,
                    TriggeredByUserAvatar = comment.Author.AvatarUrl,
                    TargetPostId = postId,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);

                var persistentDto = new NotificationDto
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    Title = entity.Title,
                    Body = entity.Body,
                    IsRead = entity.IsRead,
                    DataJson = entity.DataJson,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{postAuthorId}")
                    .SendAsync("ReceivePersistentNotification", persistentDto, cancellationToken);
            }

            // 3. Nếu là phản hồi một bình luận khác, lưu database và gửi notification cho tác giả bình luận gốc
            if (comment.Author != null && parentAuthorId.HasValue && parentAuthorId.Value != comment.Author.Id && parentAuthorId.Value != postAuthorId)
            {
                var dataJson = JsonSerializer.Serialize(new
                {
                    targetPostId = postId,
                    commentId = comment.Id,
                    actorId = comment.Author.Id,
                    actorName = authorName,
                    actorAvatar = comment.Author.AvatarUrl
                });

                var bodyText = !string.IsNullOrWhiteSpace(commentSnippet)
                    ? $"{authorName} đã trả lời bình luận của bạn: \"{commentSnippet}\""
                    : $"{authorName} đã trả lời bình luận của bạn.";

                var entity = new Notification
                {
                    UserId = parentAuthorId.Value,
                    Type = "comment",
                    Title = "Phản hồi bình luận",
                    Body = bodyText,
                    DataJson = dataJson,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var notification = new UserNotificationEventDto
                {
                    Id = entity.Id,
                    Type = "comment",
                    Message = entity.Body,
                    TriggeredByUserId = comment.Author.Id,
                    TriggeredByUserName = authorName,
                    TriggeredByUserAvatar = comment.Author.AvatarUrl,
                    TargetPostId = postId,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{parentAuthorId.Value}")
                    .SendAsync("ReceiveNotification", notification, cancellationToken);

                var persistentDto = new NotificationDto
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    Title = entity.Title,
                    Body = entity.Body,
                    IsRead = entity.IsRead,
                    DataJson = entity.DataJson,
                    CreatedAtUtc = entity.CreatedAtUtc
                };

                await _hubContext.Clients.Group($"user_{parentAuthorId.Value}")
                    .SendAsync("ReceivePersistentNotification", persistentDto, cancellationToken);
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
            var dataJson = JsonSerializer.Serialize(new
            {
                targetPostId = notification.TargetPostId,
                actorId = notification.TriggeredByUserId,
                actorName = notification.TriggeredByUserName,
                actorAvatar = notification.TriggeredByUserAvatar
            });

            var entity = new Notification
            {
                UserId = userId,
                Type = notification.Type,
                Title = notification.Type == "follow" ? "Người theo dõi mới" : "Thông báo mới",
                Body = notification.Message,
                DataJson = dataJson,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            notification.Id = entity.Id;

            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            var persistentDto = new NotificationDto
            {
                Id = entity.Id,
                Type = entity.Type,
                Title = entity.Title,
                Body = entity.Body,
                IsRead = entity.IsRead,
                DataJson = entity.DataJson,
                CreatedAtUtc = entity.CreatedAtUtc
            };

            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceivePersistentNotification", persistentDto, cancellationToken);
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
