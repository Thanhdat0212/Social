using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class InterestCandidateGenerator : ICandidateGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InterestCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Interest;
    public int Priority => 10;

    public InterestCandidateGenerator(
        IUnitOfWork unitOfWork,
        ILogger<InterestCandidateGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandidatePostDto>> GenerateCandidatesAsync(
        RecommendationContext context,
        int targetCount,
        CancellationToken cancellationToken = default)
    {
        if (targetCount <= 0) targetCount = 300;

        try
        {
            List<Guid> targetInterestIds = new();

            if (context.UserId.HasValue)
            {
                if (context.UserPreferenceScores != null && context.UserPreferenceScores.Count > 0)
                {
                    targetInterestIds = context.UserPreferenceScores
                        .Where(kv => kv.Value > 0)
                        .OrderByDescending(kv => kv.Value)
                        .Take(10)
                        .Select(kv => kv.Key)
                        .ToList();
                }
                else
                {
                    var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(context.UserId.Value, cancellationToken);
                    if (preferences.Count > 0)
                    {
                        targetInterestIds = preferences
                            .Where(p => p.Score > 0)
                            .OrderByDescending(p => p.Score)
                            .Take(10)
                            .Select(p => p.InterestId)
                            .ToList();
                    }
                }

                // Fallback sang UserInterests nếu preferences chưa có hoặc ít
                if (targetInterestIds.Count == 0)
                {
                    if (context.UserInterestIds != null && context.UserInterestIds.Count > 0)
                    {
                        targetInterestIds = context.UserInterestIds.ToList();
                    }
                    else
                    {
                        var staticInterests = await _unitOfWork.UserInterests.GetByUserIdAsync(context.UserId.Value, cancellationToken);
                        targetInterestIds = staticInterests.Select(ui => ui.InterestId).ToList();
                    }
                }
            }

            var posts = targetInterestIds.Count > 0
                ? await _unitOfWork.Posts.GetInterestCandidatesAsync(targetInterestIds, targetCount, cancellationToken)
                : await _unitOfWork.Posts.GetRecentCandidatesAsync(targetCount, cancellationToken);

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
                    PrimaryInterestName = primaryPi?.Name,
                    SourceWeight = 1.0
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên Interest.");
            return Array.Empty<CandidatePostDto>();
        }
    }
}
