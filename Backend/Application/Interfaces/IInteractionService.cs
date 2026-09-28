using Application.DTOs.Interactions;

namespace Application.Interfaces;

public interface IInteractionService
{
    Task TrackInteractionAsync(TrackInteractionRequestDto request, CancellationToken cancellationToken = default);
    Task TrackBatchInteractionsAsync(BatchTrackInteractionsRequestDto request, CancellationToken cancellationToken = default);
}
