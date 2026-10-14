namespace Application.Common;

/// <summary>
/// Dịch vụ mã hóa và băm bảo mật dành cho kho hàng số (AES-256-GCM + Salted SHA-256)
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Mã hóa chuỗi nhạy cảm bằng thuật toán AES-256-GCM
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Giải mã chuỗi AES-256-GCM ra văn bản gốc
    /// </summary>
    string Decrypt(string cipherTextBase64);

    /// <summary>
    /// Tính toán chuỗi băm SHA-256 có Salt để kiểm tra chống nạp trùng mã trong cơ sở dữ liệu
    /// </summary>
    string ComputeHash(string plainText);

    /// <summary>
    /// Tạo chuỗi che giấu ký tự (VD: "***-1A2B") để Seller nhận diện trong giao diện kho
    /// </summary>
    string MaskValue(string plainText);
}
