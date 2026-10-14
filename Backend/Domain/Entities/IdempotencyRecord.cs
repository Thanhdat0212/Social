using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Bản ghi lưu kết quả xử lý của các request có Idempotency-Key
/// Dùng để chống lặp request (bấm đúp, webhook retry trùng lặp)
/// </summary>
public class IdempotencyRecord : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
}
