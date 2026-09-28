namespace Application.DTOs.Interests;

public class UserPreferenceDto
{
    public Guid InterestId { get; set; }
    public string InterestName { get; set; } = string.Empty;
    public string InterestSlug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public double Score { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
