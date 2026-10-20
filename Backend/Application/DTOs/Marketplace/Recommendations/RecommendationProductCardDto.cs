using Application.DTOs.Marketplace.Products;

namespace Application.DTOs.Marketplace.Recommendations;

/// <summary>
/// DTO hiển thị thẻ sản phẩm gợi ý trên Marketplace, kế thừa CatalogProductCardDto
/// Bổ sung thông tin giải thích lý do gợi ý (Explainable AI) và điểm xếp hạng
/// </summary>
public class RecommendationProductCardDto : CatalogProductCardDto
{
    /// <summary>
    /// Lý do sản phẩm này được đề xuất cho người dùng (ví dụ: "Dựa trên sở thích Lập trình", "Thường mua kèm", "Bán chạy nhất tuần")
    /// </summary>
    public string RecommendationReason { get; set; } = string.Empty;

    /// <summary>
    /// Điểm số xếp hạng của sản phẩm trong thuật toán đề xuất
    /// </summary>
    public double RecommendationScore { get; set; } = 0.0;

    /// <summary>
    /// Chiến lược sinh ứng viên đề xuất (SocialAffinity, RecentIntent, ContentSimilarity, BoughtTogether, Trending)
    /// </summary>
    public string Strategy { get; set; } = string.Empty;
}
