using Application.Common;
using Application.DTOs.Follows;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FollowService : IFollowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<FollowService> _logger;

    public FollowService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<FollowService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<FollowToggleResponseDto> ToggleFollowAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == targetUserId)
        {
            throw new ValidationAppException("TargetUserId", "Bạn không thể tự theo dõi chính mình.");
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            throw new NotFoundException("Người dùng cần theo dõi không tồn tại.");
        }

        var existingFollow = await _unitOfWork.UserFollows.GetFollowAsync(currentUserId, targetUserId, cancellationToken);
        bool isFollowing;

        if (existingFollow != null)
        {
            // Unfollow
            _unitOfWork.UserFollows.Delete(existingFollow);
            isFollowing = false;
        }
        else
        {
            // Follow
            var follow = new UserFollow
            {
                FollowerId = currentUserId,
                FollowingId = targetUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.UserFollows.AddAsync(follow, cancellationToken);
            isFollowing = true;

            // Ghi nhận hành vi UserInteraction
            await _unitOfWork.UserInteractions.AddAsync(new UserInteraction
            {
                UserId = currentUserId,
                InteractionType = InteractionType.Follow,
                Value = 8.0,
                Metadata = targetUserId.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var followersCount = await _unitOfWork.UserFollows.GetFollowersCountAsync(targetUserId, cancellationToken);

        _logger.LogInformation(
            "Người dùng {UserId} đã {Action} người dùng {TargetUserId}.",
            currentUserId, isFollowing ? "theo dõi" : "bỏ theo dõi", targetUserId);

        return new FollowToggleResponseDto
        {
            TargetUserId = targetUserId,
            IsFollowing = isFollowing,
            FollowersCount = followersCount
        };
    }

    public async Task<FollowStatsDto> GetUserFollowStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var followersCount = await _unitOfWork.UserFollows.GetFollowersCountAsync(userId, cancellationToken);
        var followingCount = await _unitOfWork.UserFollows.GetFollowingCountAsync(userId, cancellationToken);
        
        bool isFollowingByCurrentUser = false;
        if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
        {
            isFollowingByCurrentUser = await _unitOfWork.UserFollows.IsFollowingAsync(
                _currentUserService.UserId.Value, userId, cancellationToken);
        }

        return new FollowStatsDto
        {
            UserId = userId,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowingByCurrentUser = isFollowingByCurrentUser
        };
    }

    public async Task<IReadOnlyList<UserFollowDto>> GetFollowersAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var follows = await _unitOfWork.UserFollows.GetFollowersAsync(userId, page, pageSize, cancellationToken);

        return follows.Select(f => new UserFollowDto
        {
            Id = f.Follower.Id,
            DisplayName = f.Follower.DisplayName,
            Bio = f.Follower.Bio,
            AvatarUrl = f.Follower.AvatarUrl,
            FollowedAtUtc = f.CreatedAtUtc
        }).ToList();
    }

    public async Task<IReadOnlyList<UserFollowDto>> GetFollowingAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var follows = await _unitOfWork.UserFollows.GetFollowingAsync(userId, page, pageSize, cancellationToken);

        return follows.Select(f => new UserFollowDto
        {
            Id = f.Following.Id,
            DisplayName = f.Following.DisplayName,
            Bio = f.Following.Bio,
            AvatarUrl = f.Following.AvatarUrl,
            FollowedAtUtc = f.CreatedAtUtc
        }).ToList();
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
