namespace Application.Common;

/// <summary>
/// Hợp đồng quản lý bộ nhớ đệm phân tán (Distributed Cache với Redis / In-Memory Cache)
/// Đảm bảo nguyên tắc Clean Architecture: Tầng Application chỉ phụ thuộc interface, không dính dáng tới thư viện Redis.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Lấy giá trị trong cache theo key, trả về null nếu cache miss hoặc Redis mất kết nối
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pattern Cache-Aside cốt lõi:
    /// 1. Kiểm tra cache có sẵn dữ liệu hay không (Cache HIT).
    /// 2. Nếu không có (Cache MISS): chạy hàm factory lấy dữ liệu từ Database, lưu vào Cache rồi trả về.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu giá trị trực tiếp vào cache kèm thời gian hết hạn (Async)
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu giá trị trực tiếp vào cache (Sync wrapper thuận tiện)
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Xóa một key cụ thể khỏi cache khi dữ liệu bị thay đổi (Cache Invalidation Async)
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một key cụ thể khỏi cache (Sync wrapper)
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Xóa toàn bộ các key có tiền tố prefix (Ví dụ: "categories" -> xóa hết "social:categories:*") (Async)
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa toàn bộ các key có tiền tố prefix (Sync wrapper)
    /// </summary>
    void RemoveByPrefix(string prefix);
}
