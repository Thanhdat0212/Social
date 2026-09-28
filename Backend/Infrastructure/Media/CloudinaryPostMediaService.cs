using Application.Interfaces;
using Application.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media;

public class CloudinaryPostMediaService : IPostMediaStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryPostMediaService> _logger;

    public CloudinaryPostMediaService(
        IOptions<CloudinarySettings> cloudinarySettings,
        ILogger<CloudinaryPostMediaService> logger)
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

    public async Task<(string Url, string PublicId)> UploadPostMediaAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var publicId = $"posts/{userId}/{Guid.NewGuid()}";
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
                _logger.LogError("Cloudinary post media upload failed: {Message}", uploadResult.Error.Message);
                throw new ValidationAppException("Media", $"Tải ảnh bài viết thất bại: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty, uploadResult.PublicId);
        }
        catch (ValidationAppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi bất ngờ khi tải ảnh bài viết lên Cloudinary cho user {UserId}", userId);
            throw new ValidationAppException("Media", "Không thể tải ảnh bài viết lên dịch vụ lưu trữ. Vui lòng thử lại sau.");
        }
    }
}
