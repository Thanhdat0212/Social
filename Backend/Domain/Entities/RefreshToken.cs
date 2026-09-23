using Domain.Common;

namespace Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty; // Hash Token để luu token vào db
    public DateTime ExpiresAtUtc { get; set; } // Thời điểm hết hạn của Token
    public string? CreatedByIp { get; set; }// Lưu IP tại thời điểm tạo refresh token
    public DateTime? RevokedAtUtc { get; set; } // Thời điểm bị thu hồi
    public string? ReplacedByTokenHash { get; set; } // Hash Token mới thay thế token cũ

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc; // Kiểm tra xem token đã hết hạn chưa chưa 
    public bool IsRevoked => RevokedAtUtc != null; // Kiểm tra xem token đã bị thu hồi chưa
    public bool IsActive => !IsRevoked && !IsExpired; // Kiểm tra xem token có còn hoạt động không 

    // Navigation property
    public User User { get; set; } = null!;
}
