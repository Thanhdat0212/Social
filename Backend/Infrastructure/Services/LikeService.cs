using Application.Common;
using Application.DTOs.Likes;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LikeService : ILikeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<LikeService> _logger;

    public LikeService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<LikeService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    public async Task<LikeToggleResponseDto> ToggleLikeAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var post = await _unitOfWork.Posts.GetWithDetailsByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Bài viết không tồn tại.");
        }

        var existingLike = await _unitOfWork.PostLikes.GetLikeAsync(currentUserId, postId, cancellationToken);
        bool isLiked;

        if (existingLike != null)
        {
            // Unlike
            _unitOfWork.PostLikes.Delete(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
            isLiked = false;

            // Giảm nhẹ điểm sở thích khi unlike
            foreach (var postInterest in post.PostInterests)
            {
                var scoreDelta = -0.5 * postInterest.Confidence;
                await _unitOfWork.UserPreferences.UpsertPreferenceAsync(
                    currentUserId,
                    postInterest.InterestId,
                    scoreDelta,
                    cancellationToken);
            }
        }
        else
        {
            // Like
            var like = new PostLike
            {
                UserId = currentUserId,
                PostId = postId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.PostLikes.AddAsync(like, cancellationToken);
            post.LikeCount++;
            isLiked = true;

            // Ghi nhận hành vi UserInteraction
            await _unitOfWork.UserInteractions.AddAsync(new UserInteraction
            {
                UserId = currentUserId,
                PostId = postId,
                InteractionType = InteractionType.Like,
                Value = 5.0,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Cập nhật tăng điểm sở thích động cho các topic của bài viết
            foreach (var postInterest in post.PostInterests)
            {
                var scoreDelta = 0.5 * postInterest.Confidence;
                await _unitOfWork.UserPreferences.UpsertPreferenceAsync(
                    currentUserId,
                    postInterest.InterestId,
                    scoreDelta,
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Phát sự kiện realtime cập nhật Like cho bài viết và gửi thông báo cho tác giả
        await _realtimeNotificationService.PublishPostLikedAsync(postId, post.LikeCount, currentUserId, isLiked, post.AuthorId, cancellationToken);

        _logger.LogInformation(
            "Người dùng {UserId} đã {Action} bài viết {PostId}. Tổng lượt like: {Count}",
            currentUserId, isLiked ? "thích" : "bỏ thích", postId, post.LikeCount);

        return new LikeToggleResponseDto
        {
            PostId = postId,
            IsLiked = isLiked,
            LikeCount = post.LikeCount
        };
    }

    public async Task<bool> IsPostLikedAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return false;
        }

        return await _unitOfWork.PostLikes.IsLikedAsync(_currentUserService.UserId.Value, postId, cancellationToken);
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
