using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class UserInteraction : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? PostId { get; set; }
    public Post? Post { get; set; }

    public InteractionType InteractionType { get; set; }
    public double Value { get; set; } = 1.0;
    public string? Metadata { get; set; }
}
