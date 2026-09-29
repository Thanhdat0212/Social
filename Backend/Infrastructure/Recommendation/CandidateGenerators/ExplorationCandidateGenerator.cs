using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class ExplorationCandidateGenerator : ICandidateGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExplorationCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Exploration;
    public int Priority => 40;

    public ExplorationCandidateGenerator(
        IUnitOfWork unitOfWork,
        ILogger<ExplorationCandidateGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandidatePostDto>> GenerateCandidatesAsync(
        RecommendationContext context,
        int targetCount,
        CancellationToken cancellationToken = default)
    {
        if (targetCount <= 0) targetCount = 100;

        try
        {
            List<Guid> knownInterestIds = new();
            if (context.UserId.HasValue)
            {
                var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(context.UserId.Value, cancellationToken);
                knownInterestIds = preferences.Select(p => p.InterestId).ToList();
            }

            var posts = await _unitOfWork.Posts.GetExplorationPostsAsync(
                knownInterestIds,
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
                    SourceWeight = 0.95
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên Exploration.");
            return Array.Empty<CandidatePostDto>();
        }
    }
}
