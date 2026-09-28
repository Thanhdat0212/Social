using Domain.Common;

namespace Domain.Entities;

public class Interest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    public ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
    public ICollection<PostInterest> PostInterests { get; set; } = new List<PostInterest>();
}
