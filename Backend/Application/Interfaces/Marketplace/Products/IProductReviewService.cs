using Application.DTOs.Marketplace.Products;

namespace Application.Interfaces.Marketplace.Products;

public interface IProductReviewService
{
    Task<ProductReviewDto> CreateReviewAsync(
        Guid buyerUserId,
        CreateProductReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ProductReviewDto>> GetReviewsByProductIdAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProductReviewSummaryDto> GetReviewSummaryByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductReviewDto> SellerReplyAsync(
        Guid sellerUserId,
        Guid reviewId,
        SellerReplyReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> HasReviewedOrderItemAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default);
}
