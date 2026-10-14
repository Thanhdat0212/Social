using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Shops;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Đánh giá và nhận xét sản phẩm từ người mua hàng thực tế (Verified Purchase)
/// </summary>
public class ProductReview : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid ShopId { get; private set; }
    public Guid BuyerUserId { get; private set; }
    public Guid ShopOrderId { get; private set; }
    public Guid OrderItemId { get; private set; }

    /// <summary>
    /// Điểm đánh giá (1 đến 5 sao)
    /// </summary>
    public int Rating { get; private set; }

    /// <summary>
    /// Nội dung nhận xét chi tiết
    /// </summary>
    public string Comment { get; private set; } = string.Empty;

    /// <summary>
    /// Xác nhận người mua hàng thật
    /// </summary>
    public bool IsVerifiedPurchase { get; private set; } = true;

    /// <summary>
    /// Phản hồi từ chủ gian hàng
    /// </summary>
    public string? SellerReply { get; private set; }
    public DateTime? SellerRepliedAtUtc { get; private set; }

    /// <summary>
    /// Bị ẩn bởi Quản trị viên nếu vi phạm tiêu chuẩn cộng đồng
    /// </summary>
    public bool IsHidden { get; private set; } = false;

    // Navigation properties
    public Product? Product { get; private set; }
    public Shop? Shop { get; private set; }
    public User? BuyerUser { get; private set; }
    public ShopOrder? ShopOrder { get; private set; }
    public OrderItem? OrderItem { get; private set; }

    protected ProductReview() { }

    public ProductReview(
        Guid productId,
        Guid shopId,
        Guid buyerUserId,
        Guid shopOrderId,
        Guid orderItemId,
        int rating,
        string comment)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (buyerUserId == Guid.Empty) throw new ArgumentException("BuyerUserId không hợp lệ.", nameof(buyerUserId));
        if (shopOrderId == Guid.Empty) throw new ArgumentException("ShopOrderId không hợp lệ.", nameof(shopOrderId));
        if (orderItemId == Guid.Empty) throw new ArgumentException("OrderItemId không hợp lệ.", nameof(orderItemId));

        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Điểm đánh giá phải từ 1 đến 5 sao.");
        }

        ProductId = productId;
        ShopId = shopId;
        BuyerUserId = buyerUserId;
        ShopOrderId = shopOrderId;
        OrderItemId = orderItemId;
        Rating = rating;
        Comment = comment?.Trim() ?? string.Empty;
        IsVerifiedPurchase = true;
        IsHidden = false;
    }

    public void AddSellerReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("Nội dung phản hồi không được để trống.", nameof(reply));
        }

        SellerReply = reply.Trim();
        SellerRepliedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetHidden(bool isHidden)
    {
        IsHidden = isHidden;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
