using Application.Common;

namespace Infrastructure.Common;

/// <summary>
/// Cài đặt IClock lấy thời gian DateTime.UtcNow thực tế
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
