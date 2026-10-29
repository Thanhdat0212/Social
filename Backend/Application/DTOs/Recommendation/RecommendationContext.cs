namespace Application.DTOs.Recommendation;

public class RecommendationContext
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
    public HashSet<Guid> SeenPostIds { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TargetCandidateCount { get; set; } = 700;

    /// <summary>Cache điểm sở thích của user mục tiêu để tái sử dụng xuyên suốt pipeline.</summary>
    public Dictionary<Guid, double>? UserPreferenceScores { get; set; }

    /// <summary>Danh sách sở thích tĩnh của user mục tiêu (nếu chưa có preference động).</summary>
    public IReadOnlyList<Guid>? UserInterestIds { get; set; }

    /// <summary>Danh sách ID các bài viết user đã xem gần đây.</summary>
    public IReadOnlyList<Guid>? ViewedPostIds { get; set; }

    /// <summary>Danh sách ID người dùng mà user đang theo dõi.</summary>
    public IReadOnlyList<Guid>? FollowingUserIds { get; set; }
}
