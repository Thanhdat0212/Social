namespace Infrastructure.Marketplace.Recommendations.Generators;

/// <summary>
/// Mô hình ứng viên sản phẩm trung gian trong luồng xử lý Recommendation Engine
/// Chứa metadata rút gọn và điểm số cơ sở của từng chiến lược sinh ứng viên
/// </summary>
public class MarketplaceCandidateItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CoverImageUrl { get; set; }
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Điểm số ban đầu do Generator tính toán dựa trên mức độ phù hợp tín hiệu
    /// </summary>
    public double BaseScore { get; set; } = 1.0;

    /// <summary>
    /// Tên chiến lược sinh ứng viên (SocialAffinity, RecentIntent, ContentSimilarity, Trending)
    /// </summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// Lý do đề xuất phục vụ Explainable AI (vd: "Dựa trên sở thích của bạn")
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
