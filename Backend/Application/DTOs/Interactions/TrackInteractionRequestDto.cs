using Domain.Enums;

namespace Application.DTOs.Interactions;

public class TrackInteractionRequestDto
{
    public Guid? PostId { get; set; }
    public InteractionType InteractionType { get; set; }
    public double Value { get; set; } = 1.0;
    public string? Metadata { get; set; }
}
