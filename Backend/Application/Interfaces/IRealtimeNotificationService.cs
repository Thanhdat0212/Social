using Application.DTOs.Comments;
using Application.DTOs.Posts;
using Application.DTOs.Realtime;

namespace Application.Interfaces;

public interface IRealtimeNotificationService
{
    /// <summary>
    /// Phát sự kiện khi có bài viết mới được xuất bản tới toàn bộ các client đang kết nối
    /// </summary>
    Task PublishNewPostAsync(PostDto post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phát sự kiện cập nhật số lượng Like của một bài viết tới nhóm xem bài viết và gửi thông báo cho tác giả
    /// </summary>
    Task PublishPostLikedAsync(Guid postId, int likeCount, Guid userId, bool isLiked, Guid postAuthorId, 
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Phát sự kiện khi có bình luận mới tới nhóm xem bài viết và gửi thông báo cho tác giả
    /// </summary>
    Task PublishCommentAddedAsync(Guid postId, int totalCommentCount, CommentDto comment, Guid postAuthorId, 
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi thông báo trực tiếp tới một User cụ thể
    /// </summary>
    Task SendNotificationToUserAsync(Guid userId, UserNotificationEventDto notification, 
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Phát sự kiện khi một bài viết bị xóa để các client lập tức loại bỏ khỏi bảng tin
    /// </summary>
    Task PublishPostDeletedAsync(Guid postId, CancellationToken cancellationToken = default);
}
