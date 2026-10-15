using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class VerificationToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public VerificationPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc != null;
    public bool IsValid => !IsConsumed && !IsExpired;

    // Navigation property
    public User User { get; set; } = null!;
}
