using Domain.Entities.Marketplace.Products;

namespace Domain.Entities.Marketplace.Recommendations;

/// <summary>
/// Thực thể lưu trữ các cặp sản phẩm thường được mua cùng nhau trong cùng một đơn hàng
/// Được tính toán trước (offline batch computed) bởi Background Job định kỳ để tránh tải runtime
/// </summary>
public class ProductCoOccurrence
{
    /// <summary>
    /// ID sản phẩm gốc (sản phẩm đang được xem hoặc đã mua)
    /// </summary>
    public Guid ProductAId { get; set; }
    public Product ProductA { get; set; } = null!;

    /// <summary>
    /// ID sản phẩm thường được mua kèm cùng sản phẩm gốc
    /// </summary>
    public Guid ProductBId { get; set; }
    public Product ProductB { get; set; } = null!;

    /// <summary>
    /// Số lần hai sản phẩm này cùng xuất hiện trong một đơn hàng hoàn tất
    /// </summary>
    public int CoOccurrenceCount { get; set; } = 1;

    /// <summary>
    /// Điểm số tương quan chuẩn hóa (Jaccard Similarity hoặc Cosine)
    /// </summary>
    public double Score { get; set; } = 0.0;

    /// <summary>
    /// Thời điểm cập nhật điểm số lần cuối
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
