using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.CandidateGenerators;

public class RecentCandidateGenerator : ICandidateGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecentCandidateGenerator> _logger;

    public CandidateSource Source => CandidateSource.Recent;
    public int Priority => 5; // Độ ưu tiên cao nhất, chạy đầu tiên

    public RecentCandidateGenerator(
        IUnitOfWork unitOfWork,
        ILogger<RecentCandidateGenerator> logger)
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
            // Lấy danh sách các bài viết mới nhất trên toàn hệ thống (không lọc sở thích để bài mới luôn có cơ hội hiển thị)
            var posts = await _unitOfWork.Posts.GetRecentPostsAsync(1, targetCount, cancellationToken);
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
                    SourceWeight = 1.30 // Ưu tiên trọng số cao cho bài viết mới trên bảng tin
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi sinh ứng viên Recent Posts.");
            return Array.Empty<CandidatePostDto>();
        }
    }
}
