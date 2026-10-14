using System.Security.Cryptography;
using System.Text;
using Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security;

public class AesGcmCryptoService : ICryptoService
{
    private readonly byte[] _key;
    private readonly byte[] _hashSalt;
    private readonly ILogger<AesGcmCryptoService> _logger;

    public AesGcmCryptoService(IConfiguration configuration, ILogger<AesGcmCryptoService> logger)
    {
        _logger = logger;

        // 1. Khóa mã hóa AES-256 (32 bytes)
        var keyConfig = configuration["Marketplace:EncryptionKey"] ?? configuration["Marketplace__EncryptionKey"];
        if (!string.IsNullOrWhiteSpace(keyConfig))
        {
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyConfig));
        }
        else
        {
            _logger.LogWarning("Chưa cấu hình Marketplace:EncryptionKey. Hệ thống đang sử dụng khóa dự phòng môi trường Development.");
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("SocialMarketplaceSecretEncryptionKey2026DevEnvironmentFallbackOnly"));
        }

        // 2. Muối băm (Salt) cho SHA-256
        var saltConfig = configuration["Marketplace:HashSalt"] ?? configuration["Marketplace__HashSalt"];
        if (!string.IsNullOrWhiteSpace(saltConfig))
        {
            _hashSalt = Encoding.UTF8.GetBytes(saltConfig);
        }
        else
        {
            _hashSalt = Encoding.UTF8.GetBytes("SocialMarketplaceDevSalt_2026_Unique");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Ghép chuỗi: [Nonce 12B][Tag 16B][Ciphertext NB]
        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string cipherTextBase64)
    {
        if (string.IsNullOrEmpty(cipherTextBase64)) return string.Empty;

        var combined = Convert.FromBase64String(cipherTextBase64);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        if (combined.Length < nonceSize + tagSize)
        {
            throw new CryptographicException("Dữ liệu mã hóa không đúng định dạng AES-GCM.");
        }

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var cipherBytes = new byte[combined.Length - nonceSize - tagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(combined, nonceSize + tagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string ComputeHash(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        // Dùng HMAC-SHA256 với Salt làm secret key để băm an toàn
        using var hmac = new HMACSHA256(_hashSalt);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText.Trim()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string MaskValue(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return "***";
        var trimmed = plainText.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        var last4 = trimmed[^4..];
        var maskLength = Math.Min(trimmed.Length - 4, 6);
        return $"{new string('*', maskLength)}-{last4}";
    }
}
