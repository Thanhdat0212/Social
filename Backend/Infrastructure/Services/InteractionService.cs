using Application.Common;
using Application.DTOs.Interactions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class InteractionService : IInteractionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InteractionService> _logger;

    public InteractionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<InteractionService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task TrackInteractionAsync(TrackInteractionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return;
        }

        var userId = _currentUserService.UserId.Value;
        await ProcessSingleInteractionAsync(userId, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackBatchInteractionsAsync(BatchTrackInteractionsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return;
        }

        if (request.Events == null || request.Events.Count == 0)
        {
            return;
        }

        var userId = _currentUserService.UserId.Value;

        foreach (var item in request.Events.Take(100))
        {
            await ProcessSingleInteractionAsync(userId, item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSingleInteractionAsync(Guid userId, TrackInteractionRequestDto item, CancellationToken cancellationToken)
    {
        if (item.PostId.HasValue)
        {
            // Tránh spam sự kiện View liên tục trong thời gian ngắn (5 phút)
            if (item.InteractionType == InteractionType.View)
            {
                var recentlyViewed = await _unitOfWork.UserInteractions.HasInteractedRecentlyAsync(
                    userId, item.PostId.Value, InteractionType.View, TimeSpan.FromMinutes(5), cancellationToken);

                if (recentlyViewed)
                {
                    return; // Đã ghi nhận view trong 5 phút qua
                }

                var post = await _unitOfWork.Posts.GetByIdAsync(item.PostId.Value, cancellationToken);
                if (post != null)
                {
                    post.ViewCount++;
                }
            }

            // Ghi nhận tương tác
            await _unitOfWork.UserInteractions.AddAsync(new UserInteraction
            {
                UserId = userId,
                PostId = item.PostId.Value,
                InteractionType = item.InteractionType,
                Value = item.Value > 0 ? item.Value : 1.0,
                Metadata = item.Metadata,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Cập nhật điểm sở thích tương ứng nếu bài viết có topics
            var postWithTopics = await _unitOfWork.Posts.GetWithDetailsByIdAsync(item.PostId.Value, cancellationToken);
            if (postWithTopics != null && postWithTopics.PostInterests.Count > 0)
            {
                double weightMultiplier = item.InteractionType switch
                {
                    InteractionType.View => 0.1,
                    InteractionType.Share => 1.5,
                    InteractionType.Save => 2.0,
                    InteractionType.Skip => -0.1,
                    InteractionType.NotInterested => -1.0,
                    _ => 0.05
                };

                foreach (var postInterest in postWithTopics.PostInterests)
                {
                    var delta = weightMultiplier * postInterest.Confidence;
                    await _unitOfWork.UserPreferences.UpsertPreferenceAsync(
                        userId,
                        postInterest.InterestId,
                        delta,
                        cancellationToken);
                }
            }
        }
    }
}
