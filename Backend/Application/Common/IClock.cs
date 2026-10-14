namespace Application.Common;

/// <summary>
/// Đồng hồ thời gian hệ thống dùng chung, hỗ trợ test và đồng nhất thời gian UTC
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
