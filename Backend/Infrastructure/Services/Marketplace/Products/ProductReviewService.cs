using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Products;
using Domain.Enums;
using Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Products;

public class ProductReviewService : IProductReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductReviewService> _logger;

    public ProductReviewService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProductReviewDto> CreateReviewAsync(
        Guid buyerUserId,
        CreateProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Rating), "Số sao đánh giá phải từ 1 đến 5.");
        }

        // 1. Kiểm tra đơn hàng và món hàng
        var order = await _unitOfWork.Orders.GetByOrderItemIdAsync(request.OrderItemId, cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin đơn hàng của món hàng này.");
        }

        if (order.BuyerUserId != buyerUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền đánh giá món hàng của người khác.");
        }

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed)
        {
            throw new InvalidOperationException("Chỉ có thể đánh giá sản phẩm sau khi đơn hàng đã được giao nhận thành công.");
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
        if (orderItem == null)
        {
            throw new KeyNotFoundException("Không tìm thấy món hàng trong đơn.");
        }

        // 2. Chống đánh giá trùng (Unique Verified Review)
        var alreadyReviewed = await _unitOfWork.ProductReviews.HasReviewedOrderItemAsync(request.OrderItemId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new InvalidOperationException("Bạn đã đánh giá món hàng này rồi.");
        }

        // 3. Tạo ProductReview
        var review = new ProductReview(
            productId: orderItem.ProductId,
            shopId: order.ShopId,
            buyerUserId: buyerUserId,
            shopOrderId: order.Id,
            orderItemId: orderItem.Id,
            rating: request.Rating,
            comment: request.Comment);

        await _unitOfWork.ProductReviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Tính toán lại điểm số trung bình và cập nhật vào Product
        var (average, count, _) = await _unitOfWork.ProductReviews.GetStatsByProductIdAsync(orderItem.ProductId, cancellationToken);
        var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId, cancellationToken);
        if (product != null)
        {
            product.UpdateRating(average, count);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Xóa cache chi tiết sản phẩm để người dùng xem điểm mới ngay lập tức
            await _cacheService.RemoveAsync(CacheKeys.ProductDetail(product.Slug));
        }

        var buyer = await _unitOfWork.Users.GetByIdAsync(buyerUserId, cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã gửi đánh giá {Rating} sao cho sản phẩm {ProductId}", buyerUserId, request.Rating, orderItem.ProductId);

        return new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = orderItem.ProductName,
            VariantLabel = orderItem.VariantLabel,
            ShopId = review.ShopId,
            BuyerUserId = review.BuyerUserId,
            BuyerName = string.IsNullOrWhiteSpace(buyer?.DisplayName) ? "Khách hàng" : buyer.DisplayName,
            BuyerAvatar = buyer?.AvatarUrl,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            SellerReply = review.SellerReply,
            SellerRepliedAtUtc = review.SellerRepliedAtUtc,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }

    public async Task<PagedResult<ProductReviewDto>> GetReviewsByProductIdAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var validPage = page > 0 ? page : 1;
        var validPageSize = pageSize > 0 ? pageSize : 10;

        var reviews = await _unitOfWork.ProductReviews.GetByProductIdAsync(productId, validPage, validPageSize, cancellationToken);
        var totalCount = await _unitOfWork.ProductReviews.GetCountByProductIdAsync(productId, cancellationToken);

        var items = reviews.Select(r => new ProductReviewDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = r.OrderItem?.ProductName ?? string.Empty,
            VariantLabel = r.OrderItem?.VariantLabel ?? string.Empty,
            ShopId = r.ShopId,
            BuyerUserId = r.BuyerUserId,
            BuyerName = string.IsNullOrWhiteSpace(r.BuyerUser?.DisplayName) ? "Khách hàng" : r.BuyerUser.DisplayName,
            BuyerAvatar = r.BuyerUser?.AvatarUrl,
            Rating = r.Rating,
            Comment = r.Comment,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            SellerReply = r.SellerReply,
            SellerRepliedAtUtc = r.SellerRepliedAtUtc,
            CreatedAtUtc = r.CreatedAtUtc
        }).ToList();

        return new PagedResult<ProductReviewDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = validPage,
            PageSize = validPageSize
        };
    }

    public async Task<ProductReviewSummaryDto> GetReviewSummaryByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var (average, count, distribution) = await _unitOfWork.ProductReviews.GetStatsByProductIdAsync(productId, cancellationToken);

        return new ProductReviewSummaryDto
        {
            AverageRating = Math.Round(average, 1),
            TotalReviews = count,
            StarDistribution = distribution
        };
    }

    public async Task<ProductReviewDto> SellerReplyAsync(
        Guid sellerUserId,
        Guid reviewId,
        SellerReplyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.ProductReviews.GetByIdAsync(reviewId, cancellationToken);
        if (review == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đánh giá.");
        }

        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null || review.ShopId != shop.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền phản hồi đánh giá này.");
        }

        review.AddSellerReply(request.Reply);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.OrderItem?.ProductName ?? string.Empty,
            VariantLabel = review.OrderItem?.VariantLabel ?? string.Empty,
            ShopId = review.ShopId,
            BuyerUserId = review.BuyerUserId,
            BuyerName = string.IsNullOrWhiteSpace(review.BuyerUser?.DisplayName) ? "Khách hàng" : review.BuyerUser.DisplayName,
            BuyerAvatar = review.BuyerUser?.AvatarUrl,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            SellerReply = review.SellerReply,
            SellerRepliedAtUtc = review.SellerRepliedAtUtc,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }

    public async Task<bool> HasReviewedOrderItemAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ProductReviews.HasReviewedOrderItemAsync(orderItemId, cancellationToken);
    }
}
