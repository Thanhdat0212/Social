using Application.DTOs.Recommendation;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class SemanticCandidateGenerator : ICandidateGenerator
{
    private readonly ISemanticVectorSearchService _vectorSearchService;
    private readonly ILogger<SemanticCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Semantic;
    public int Priority => 25;

    public SemanticCandidateGenerator(
        ISemanticVectorSearchService vectorSearchService,
        ILogger<SemanticCandidateGenerator> logger)
    {
        _vectorSearchService = vectorSearchService;
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
            // 1. Xây dựng vector sở thích ngữ nghĩa của người dùng từ các bài viết đã tương tác
            var userVector = await _vectorSearchService.BuildUserInterestVectorAsync(
                context.UserId.Value, cancellationToken);

            if (userVector == null || userVector.Length == 0)
            {
                return Array.Empty<CandidatePostDto>();
            }

            // 2. Tìm kiếm các bài viết có vector embedding tương đồng ngữ nghĩa nhất
            var posts = await _vectorSearchService.FindSimilarPostsByVectorAsync(
                userVector,
                context.SeenPostIds,
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
                    SourceWeight = 1.10 // Ưu tiên nhẹ các bài viết khớp ngữ nghĩa sâu
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh ứng viên Semantic Vector cho User {UserId}.", context.UserId);
            return Array.Empty<CandidatePostDto>();
        }
    }
}
