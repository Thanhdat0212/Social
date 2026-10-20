using Application.DTOs.Marketplace.Recommendations;

namespace Application.Interfaces.Marketplace.Recommendations;

/// <summary>
/// Giao diện dịch vụ Đề xuất sản phẩm cá nhân hóa cho Marketplace
/// Kết hợp các mô hình: Social Affinity, Recent Intent, Content Similarity, Bought Together và Trending
/// </summary>
public interface IMarketplaceRecommendationService
{
    /// <summary>
    /// Lấy danh sách sản phẩm đề xuất cá nhân hóa "Dành riêng cho bạn" trên trang chủ Marketplace
    /// </summary>
    /// <param name="userId">ID người dùng (nếu đã đăng nhập)</param>
    /// <param name="sessionId">Mã phiên trình duyệt của khách (nếu chưa đăng nhập)</param>
    /// <param name="limit">Số lượng sản phẩm tối đa trả về</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RecommendationProductCardDto>> GetForYouProductsAsync(
        Guid? userId,
        string? sessionId,
        int limit = 12,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách sản phẩm tương tự (thay thế) hiển thị trên trang chi tiết sản phẩm
    /// </summary>
    /// <param name="productId">ID sản phẩm đang xem</param>
    /// <param name="limit">Số lượng sản phẩm tương tự tối đa</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RecommendationProductCardDto>> GetRelatedProductsAsync(
        Guid productId,
        int limit = 8,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách sản phẩm thường được mua cùng (Frequently Bought Together)
    /// </summary>
    /// <param name="productId">ID sản phẩm đang xem</param>
    /// <param name="limit">Số lượng sản phẩm mua kèm tối đa</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RecommendationProductCardDto>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit = 4,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách sản phẩm thịnh hành (Trending / Bán chạy / Đánh giá cao)
    /// Dùng làm Fallback cho người dùng mới (Cold-Start) hoặc lọc theo danh mục
    /// </summary>
    /// <param name="categoryId">ID danh mục (tùy chọn)</param>
    /// <param name="limit">Số lượng sản phẩm tối đa</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RecommendationProductCardDto>> GetTrendingProductsAsync(
        Guid? categoryId,
        int limit = 8,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhận từ khóa tìm kiếm của người dùng
    /// </summary>
    Task RecordSearchQueryAsync(
        Guid? userId,
        RecordSearchQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lịch sử tìm kiếm gần đây của người dùng
    /// </summary>
    Task<IReadOnlyList<SearchHistoryDto>> GetRecentSearchesAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
