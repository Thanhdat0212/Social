namespace Application.DTOs.Recommendation;

public class RecommendationContext
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
    public HashSet<Guid> SeenPostIds { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TargetCandidateCount { get; set; } = 700;
}
