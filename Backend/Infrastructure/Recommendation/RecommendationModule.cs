using Application.Interfaces.Recommendation;
using Infrastructure.Recommendation.Aggregator;
using Infrastructure.Recommendation.CandidateGenerators;
using Infrastructure.Recommendation.Collaborative;
using Infrastructure.Recommendation.Diversity;
using Infrastructure.Recommendation.Ranking;
using Infrastructure.Recommendation.Semantic;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Recommendation;

/// <summary>Đăng ký toàn bộ pipeline gợi ý nội dung (Candidate Generators, Aggregator, Ranking, Diversity, Service).</summary>
public static class RecommendationModule
{
    public static IServiceCollection AddRecommendationModule(this IServiceCollection services)
    {
        services.AddScoped<ICollaborativeFilteringService, CollaborativeFilteringService>();
        services.AddScoped<ISemanticVectorSearchService, SemanticVectorSearchService>();

        // Candidate Generators
        services.AddScoped<ICandidateGenerator, RecentCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, InterestCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, FollowingCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, TrendingCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, ExplorationCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, CollaborativeCandidateGenerator>();
        services.AddScoped<ICandidateGenerator, SemanticCandidateGenerator>();

        // Pipeline components
        services.AddScoped<ICandidateAggregator, CandidateAggregator>();
        services.AddScoped<IRecommendationRankingService, RecommendationRankingService>();
        services.AddScoped<IDiversityService, DiversityService>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        return services;
    }
}
