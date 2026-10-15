namespace Application.Interfaces;

public interface IAvatarStorageService
{
    Task<(string Url, string PublicId)> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
