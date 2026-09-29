using Domain.Entities;

namespace Application.DTOs.Recommendation;

public class ScoredCandidateDto
{
    public Post Post { get; set; } = null!;
    public double FinalScore { get; set; }
    
    // Điểm thành phần từ 0 đến 100
    public double InterestScore { get; set; }
    public double BehaviorScore { get; set; }
    public double CollaborativeScore { get; set; }
    public double SemanticScore { get; set; }
    public double FreshnessScore { get; set; }
    public double PopularityScore { get; set; }

    public Guid? PrimaryInterestId { get; set; }
    public string? PrimaryInterestName { get; set; }
}
