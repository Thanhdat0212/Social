using Domain.Common;

namespace Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarPublicId { get; set; }
    public bool EmailConfirmed { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>(); // quan hệ 1 -N 
    public ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();
    public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    public ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
    public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}
