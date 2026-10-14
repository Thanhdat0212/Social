using System.Security.Cryptography;
using System.Text;
using Application.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

public class SignedUrlService : ISignedUrlService
{
    private readonly byte[] _secretKey;

    public SignedUrlService(IConfiguration configuration)
    {
        var secret = configuration["SignedUrl:SecretKey"]
            ?? configuration["JwtSettings:SigningKey"]
            ?? "DefaultSecretKeyForSignedDownloadUrlGenerationPhase123456";

        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateSignature(Guid orderItemId, Guid buyerUserId, long expiresAtUnix)
    {
        var payload = $"{orderItemId}:{buyerUserId}:{expiresAtUnix}";
        using var hmac = new HMACSHA256(_secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifySignature(Guid orderItemId, Guid buyerUserId, long expiresAtUnix, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;

        var expectedSignature = GenerateSignature(orderItemId, buyerUserId, expiresAtUnix);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant())
        );
    }

    public string BuildDownloadUrl(string baseUrl, Guid orderItemId, Guid buyerUserId, TimeSpan validity)
    {
        var expiresAtUnix = DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds();
        var signature = GenerateSignature(orderItemId, buyerUserId, expiresAtUnix);

        var cleanBaseUrl = baseUrl.TrimEnd('/');
        return $"{cleanBaseUrl}/api/marketplace/files/download?orderItemId={orderItemId}&buyerUserId={buyerUserId}&expiresAt={expiresAtUnix}&sig={signature}";
    }
}
