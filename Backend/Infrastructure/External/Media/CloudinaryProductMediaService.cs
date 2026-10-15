using Application.Interfaces.Marketplace.Products;
using Application.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.External.Media;

public class CloudinaryProductMediaService : IProductMediaStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryProductMediaService> _logger;

    public CloudinaryProductMediaService(
        IOptions<CloudinarySettings> cloudinarySettings,
        ILogger<CloudinaryProductMediaService> logger)
    {
        _logger = logger;
        var settings = cloudinarySettings.Value;

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<(string Url, string PublicId)> UploadProductImageAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var publicId = $"marketplace/products/{userId}/{Guid.NewGuid()}";
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = publicId,
                Overwrite = false,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary product image upload failed: {Message}", uploadResult.Error.Message);
                throw new ValidationAppException("Image", $"Tải ảnh sản phẩm lên Cloudinary thất bại: {uploadResult.Error.Message}");
            }

            var secureUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;
            return (secureUrl, uploadResult.PublicId);
        }
        catch (ValidationAppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi bất ngờ khi tải ảnh sản phẩm lên Cloudinary cho user {UserId}", userId);
            throw new ValidationAppException("Image", $"Không thể tải ảnh sản phẩm lên Cloudinary: {ex.Message}");
        }
    }
}
