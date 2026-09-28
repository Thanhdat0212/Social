namespace Application.DTOs.Interactions;

public class BatchTrackInteractionsRequestDto
{
    public List<TrackInteractionRequestDto> Events { get; set; } = new();
}
