using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Recommendation;

public class CandidatePostDto
{
    public Post Post { get; set; } = null!;
    public CandidateSource Source { get; set; }
    public Guid? PrimaryInterestId { get; set; }
    public string? PrimaryInterestName { get; set; }
    public double SourceWeight { get; set; } = 1.0;
    public bool IsViewed { get; set; } = false;
}

