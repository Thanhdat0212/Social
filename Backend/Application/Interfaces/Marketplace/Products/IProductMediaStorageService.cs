namespace Application.Interfaces.Marketplace.Products;

public interface IProductMediaStorageService
{
    Task<(string Url, string PublicId)> UploadProductImageAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
