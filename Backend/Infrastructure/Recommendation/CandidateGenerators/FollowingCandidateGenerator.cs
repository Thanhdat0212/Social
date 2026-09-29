using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class FollowingCandidateGenerator : ICandidateGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FollowingCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Following;
    public int Priority => 20;

    public FollowingCandidateGenerator(
        IUnitOfWork unitOfWork,
        ILogger<FollowingCandidateGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandidatePostDto>> GenerateCandidatesAsync(
        RecommendationContext context,
        int targetCount,
        CancellationToken cancellationToken = default)
    {
        if (!context.UserId.HasValue)
        {
            return Array.Empty<CandidatePostDto>();
        }

        if (targetCount <= 0) targetCount = 100;

        try
        {
            var followingUserIds = await _unitOfWork.UserFollows.GetFollowingUserIdsAsync(context.UserId.Value, cancellationToken);
            if (followingUserIds.Count == 0)
            {
                return Array.Empty<CandidatePostDto>();
            }

            var posts = await _unitOfWork.Posts.GetFollowingPostsAsync(followingUserIds, 1, targetCount, cancellationToken);

            var result = new List<CandidatePostDto>(posts.Count);
            foreach (var post in posts)
            {
                var primaryPi = post.PostInterests
                    .OrderByDescending(pi => pi.Confidence)
                    .FirstOrDefault();

                result.Add(new CandidatePostDto
                {
                    Post = post,
                    Source = Source,
                    PrimaryInterestId = primaryPi?.InterestId,
                    PrimaryInterestName = primaryPi?.Interest?.Name,
                    SourceWeight = 1.1 // Ưu tiên nhẹ bài từ người theo dõi
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên Following.");
            return Array.Empty<CandidatePostDto>();
        }
    }
}
