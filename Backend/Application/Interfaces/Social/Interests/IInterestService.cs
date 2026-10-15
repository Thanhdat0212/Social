using Application.DTOs.Interests;

namespace Application.Interfaces;

public interface IInterestService
{
    Task<IReadOnlyList<InterestDto>> GetAllInterestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterestDto>> GetMyInterestsAsync(CancellationToken cancellationToken = default);
    Task<bool> SelectMyInterestsAsync(SelectInterestsRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPreferenceDto>> GetMyPreferencesAsync(CancellationToken cancellationToken = default);
    Task<OnboardingStatusDto> GetMyOnboardingStatusAsync(CancellationToken cancellationToken = default);
}
