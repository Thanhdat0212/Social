namespace Application.Common;

/// <summary>
/// Dịch vụ tạo và kiểm tra chữ ký xác thực cho link tải tệp tin có thời hạn ngắn (Expiring Signed URL)
/// </summary>
public interface ISignedUrlService
{
    /// <summary>
    /// Tạo mã chữ ký bảo mật HMAC-SHA256 cho tệp tin của đơn hàng
    /// </summary>
    string GenerateSignature(Guid orderItemId, Guid buyerUserId, long expiresAtUnix);

    /// <summary>
    /// Kiểm tra tính hợp lệ của chữ ký URL
    /// </summary>
    bool VerifySignature(Guid orderItemId, Guid buyerUserId, long expiresAtUnix, string signature);

    /// <summary>
    /// Xây dựng URL tải file hoàn chỉnh kèm chữ ký và thời hạn hết hạn
    /// </summary>
    string BuildDownloadUrl(string baseUrl, Guid orderItemId, Guid buyerUserId, TimeSpan validity);
}
