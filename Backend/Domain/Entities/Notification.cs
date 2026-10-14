using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Thực thể Thông báo trong hệ thống (dùng cho cả Social và Shop Marketplace)
/// </summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }
    public string? DataJson { get; set; }

    // Navigation property
    public User? User { get; set; }
}
