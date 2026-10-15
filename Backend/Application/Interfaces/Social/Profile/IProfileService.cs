using Application.DTOs.Profile.Requests;
using Application.DTOs.Profile.Responses;

namespace Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<string> UploadAvatarAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
