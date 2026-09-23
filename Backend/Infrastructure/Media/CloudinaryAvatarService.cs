using Application.Interfaces;
using Application.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media;

public class CloudinaryAvatarService : IAvatarStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryAvatarService> _logger;

    public CloudinaryAvatarService(
        IOptions<CloudinarySettings> cloudinarySettings,
        ILogger<CloudinaryAvatarService> logger)
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

    public async Task<(string Url, string PublicId)> UploadAvatarAsync(
        Guid userId, 
        Stream fileStream, 
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = $"avatars/{userId}",
                Overwrite = true,
                Transformation = new Transformation()
                    .Width(500)
                    .Height(500)
                    .Crop("fill")
                    .Gravity("face")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Message}", uploadResult.Error.Message);
                throw new ValidationAppException("Avatar", $"Tải ảnh đại diện thất bại: {uploadResult.Error.Message}");
            }

            // Trả về SecureUrl (có chứa version để chống cache trình duyệt) và PublicId
            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }
        catch (Exception ex) when (ex is not ValidationAppException)
        {
            _logger.LogError(ex, "Exception occurred during avatar upload to Cloudinary for user {UserId}", userId);
            throw new ValidationAppException("Avatar", "Có lỗi xảy ra trong quá trình lưu trữ ảnh đại diện.");
        }
    }
}
