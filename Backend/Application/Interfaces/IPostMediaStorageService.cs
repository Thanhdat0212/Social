namespace Application.Interfaces;

public interface IPostMediaStorageService
{
    Task<(string Url, string PublicId)> UploadPostMediaAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
