using Application.DTOs.Recommendation;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class CollaborativeCandidateGenerator : ICandidateGenerator
{
    private readonly ICollaborativeFilteringService _collaborativeService;
    private readonly ILogger<CollaborativeCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Collaborative;
    public int Priority => 15;

    public CollaborativeCandidateGenerator(
        ICollaborativeFilteringService collaborativeService,
        ILogger<CollaborativeCandidateGenerator> logger)
    {
        _collaborativeService = collaborativeService;
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

        if (targetCount <= 0) targetCount = 200;

        try
        {
            var posts = await _collaborativeService.GetCollaborativeCandidatesAsync(
                context.UserId.Value,
                targetCount,
                cancellationToken);

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
                    SourceWeight = 1.15 // Ưu tiên nhẹ các bài viết gợi ý từ cộng đồng
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên Collaborative Filtering cho User {UserId}.", context.UserId);
            return Array.Empty<CandidatePostDto>();
        }
    }
}
