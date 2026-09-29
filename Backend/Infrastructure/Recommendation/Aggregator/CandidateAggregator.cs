using Application.DTOs.Recommendation;
using Application.Interfaces.Repositories;
using Application.Interfaces.Recommendation;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Recommendation.Aggregator;

public class CandidateAggregator : ICandidateAggregator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CandidateAggregator> _logger;

    public CandidateAggregator(
        IUnitOfWork unitOfWork,
        ILogger<CandidateAggregator> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandidatePostDto>> AggregateAsync(
        IReadOnlyList<CandidatePostDto> allCandidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        if (allCandidates.Count == 0)
        {
            return Array.Empty<CandidatePostDto>();
        }

        // 1. Tập hợp các ID bài viết đã xem
        var viewedPostIds = new HashSet<Guid>(context.SeenPostIds);

        if (context.UserId.HasValue)
        {
            var dbViewed = await _unitOfWork.UserInteractions.GetViewedPostIdsAsync(
                context.UserId.Value, 500, cancellationToken);
            foreach (var id in dbViewed)
            {
                viewedPostIds.Add(id);
            }
        }

        // 2. Khử trùng lặp bài viết (Distinct by Post.Id)
        // Nếu bài viết xuất hiện ở nhiều generator, ưu tiên giữ nguồn có SourceWeight cao hơn
        var distinctMap = new Dictionary<Guid, CandidatePostDto>();
        foreach (var candidate in allCandidates)
        {
            if (!distinctMap.TryGetValue(candidate.Post.Id, out var existing))
            {
                distinctMap[candidate.Post.Id] = candidate;
            }
            else
            {
                // Thưởng điểm nhẹ vì bài viết xuất hiện ở nhiều kênh (Cross-Channel Boost)
                existing.SourceWeight = Math.Min(1.5, existing.SourceWeight + 0.15);
            }
        }

        var uniqueCandidates = distinctMap.Values.ToList();

        // 3. Phân nhóm: Chưa xem (Unseen) và Đã xem (Viewed)
        var unseen = uniqueCandidates.Where(c => !viewedPostIds.Contains(c.Post.Id)).ToList();
        var viewed = uniqueCandidates.Where(c => viewedPostIds.Contains(c.Post.Id)).ToList();

        // Giới hạn số lượng ứng viên đưa vào Ranking để tối ưu hiệu năng
        var targetCount = context.TargetCandidateCount > 0 ? context.TargetCandidateCount : 700;

        var finalPool = unseen.Take(targetCount).ToList();
        if (finalPool.Count < targetCount && viewed.Count > 0)
        {
            finalPool.AddRange(viewed.Take(targetCount - finalPool.Count));
        }

        return finalPool;
    }
}
