using Application.Common;
using Application.DTOs.Comments;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<CommentService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    public async Task<CommentDto> CreateCommentAsync(Guid postId, CreateCommentRequestDto request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationAppException("Content", "Nội dung bình luận không được để trống.");
        }

        var post = await _unitOfWork.Posts.GetWithDetailsByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Bài viết không tồn tại.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parent = await _unitOfWork.Comments.GetByIdAsync(request.ParentCommentId.Value, cancellationToken);
            if (parent == null || parent.PostId != postId)
            {
                throw new NotFoundException("Bình luận gốc không tồn tại trên bài viết này.");
            }
        }

        var comment = new Comment
        {
            PostId = postId,
            AuthorId = currentUserId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        post.CommentCount++;

        // Ghi nhận hành vi UserInteraction
        await _unitOfWork.UserInteractions.AddAsync(new UserInteraction
        {
            UserId = currentUserId,
            PostId = postId,
            InteractionType = InteractionType.Comment,
            Value = 10.0,
            Metadata = comment.Id.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        // Cập nhật mạnh điểm sở thích khi comment
        foreach (var postInterest in post.PostInterests)
        {
            var scoreDelta = 1.0 * postInterest.Confidence;
            await _unitOfWork.UserPreferences.UpsertPreferenceAsync(
                currentUserId,
                postInterest.InterestId,
                scoreDelta,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã bình luận vào bài viết {PostId}.", currentUserId, postId);

        var commentDto = new CommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc,
            Author = new CommentAuthorDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            },
            Replies = new List<CommentDto>()
        };

        // Phát sự kiện realtime tới room post_{postId} và gửi thông báo tới tác giả bài viết
        await _realtimeNotificationService.PublishCommentAddedAsync(postId, post.CommentCount, commentDto, post.AuthorId, cancellationToken);

        return commentDto;
    }

    public async Task<IReadOnlyList<CommentDto>> GetPostCommentsAsync(Guid postId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var comments = await _unitOfWork.Comments.GetByPostIdAsync(postId, page, pageSize, cancellationToken);

        return comments.Select(c => new CommentDto
        {
            Id = c.Id,
            PostId = c.PostId,
            ParentCommentId = c.ParentCommentId,
            Content = c.Content,
            CreatedAtUtc = c.CreatedAtUtc,
            Author = new CommentAuthorDto
            {
                Id = c.Author.Id,
                DisplayName = c.Author.DisplayName,
                AvatarUrl = c.Author.AvatarUrl
            },
            Replies = c.Replies.Select(r => new CommentDto
            {
                Id = r.Id,
                PostId = r.PostId,
                ParentCommentId = r.ParentCommentId,
                Content = r.Content,
                CreatedAtUtc = r.CreatedAtUtc,
                Author = new CommentAuthorDto
                {
                    Id = r.Author.Id,
                    DisplayName = r.Author.DisplayName,
                    AvatarUrl = r.Author.AvatarUrl
                }
            }).OrderBy(r => r.CreatedAtUtc).ToList()
        }).ToList();
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException("Bình luận không tồn tại.");
        }

        if (comment.AuthorId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
        }

        var post = await _unitOfWork.Posts.GetByIdAsync(comment.PostId, cancellationToken);
        if (post != null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount - 1);
        }

        _unitOfWork.Comments.Delete(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã xóa bình luận {CommentId}.", currentUserId, commentId);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Yêu cầu xác thực tài khoản.");
        }

        return _currentUserService.UserId.Value;
    }
}
