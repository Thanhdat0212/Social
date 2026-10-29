namespace Application.DTOs.Recommendation;

public class CandidatePostInterestDto
{
    public Guid InterestId { get; set; }
    public string? Name { get; set; }
    public double Confidence { get; set; }
}

public class PostCandidateDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }
    public List<CandidatePostInterestDto> PostInterests { get; set; } = new();
}

