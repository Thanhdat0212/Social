namespace Domain.Entities;

public class UserPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid InterestId { get; set; }
    public Interest Interest { get; set; } = null!;

    public double Score { get; set; } = 1.0;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
