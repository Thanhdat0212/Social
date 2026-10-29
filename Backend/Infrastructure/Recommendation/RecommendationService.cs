using Application.DTOs.Recommendation;
using Application.Interfaces.Recommendation;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation;

public class RecommendationService : IRecommendationService
{
    private readonly IEnumerable<ICandidateGenerator> _generators;
    private readonly ICandidateAggregator _aggregator;
    private readonly IRecommendationRankingService _rankingService;
    private readonly IDiversityService _diversityService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IEnumerable<ICandidateGenerator> generators,
        ICandidateAggregator aggregator,
        IRecommendationRankingService rankingService,
        IDiversityService diversityService,
        IServiceScopeFactory scopeFactory,
        ILogger<RecommendationService> logger)
    {
        _generators = generators;
        _aggregator = aggregator;
        _rankingService = rankingService;
        _diversityService = diversityService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Post>> GetForYouFeedPostsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Kích hoạt song song các Candidate Generators trên các Scoped DbContext độc lập
        var activeGenerators = _generators.OrderBy(g => g.Priority).ToList();
        if (activeGenerators.Count == 0)
        {
            _logger.LogWarning("Không tìm thấy CandidateGenerator nào được cấu hình trong hệ thống.");
            return Array.Empty<Post>();
        }

        var tasks = activeGenerators.Select(async generatorMetadata =>
        {
            int quota = generatorMetadata.Source switch
            {
                CandidateSource.Recent => 100,
                CandidateSource.Interest => 300,
                CandidateSource.Following => 100,
                CandidateSource.Trending => 100,
                CandidateSource.Exploration => 100,
                CandidateSource.Collaborative => 200,
                CandidateSource.Semantic => 200,
                _ => 100
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedGenerator = scope.ServiceProvider.GetServices<ICandidateGenerator>()
                    .First(g => g.Source == generatorMetadata.Source);

                return await scopedGenerator.GenerateCandidatesAsync(context, quota, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên từ generator {Source}.", generatorMetadata.Source);
                return (IReadOnlyList<CandidatePostDto>)Array.Empty<CandidatePostDto>();
            }
        });

        var results = await Task.WhenAll(tasks);
        var allCandidates = new List<CandidatePostDto>();
        foreach (var candidateBatch in results)
        {
            if (candidateBatch.Count > 0)
            {
                allCandidates.AddRange(candidateBatch);
            }
        }

        if (allCandidates.Count == 0)
        {
            return Array.Empty<Post>();
        }

        // 2. Tổng hợp, khử trùng lặp và loại bài đã xem (Candidate Aggregation)
        var aggregatedCandidates = await _aggregator.AggregateAsync(allCandidates, context, cancellationToken);
        if (aggregatedCandidates.Count == 0)
        {
            return Array.Empty<Post>();
        }

        // 3. Xếp hạng đa yếu tố (Multi-factor Ranking)
        var rankedCandidates = await _rankingService.RankAsync(aggregatedCandidates, context, cancellationToken);

        // 4. Đa dạng hóa feed theo chủ đề (Diversity Re-ranking)
        var diversifiedPosts = _diversityService.ApplyDiversity(rankedCandidates, maxConsecutiveSameTopic: 2);

        return diversifiedPosts;
    }
}
