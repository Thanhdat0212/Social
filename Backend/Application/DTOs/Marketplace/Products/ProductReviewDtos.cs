namespace Application.DTOs.Marketplace.Products;

public class CreateProductReviewRequest
{
    public Guid OrderItemId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class SellerReplyReviewRequest
{
    public string Reply { get; set; } = string.Empty;
}

public class ProductReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public Guid BuyerUserId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerAvatar { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public string? SellerReply { get; set; }
    public DateTime? SellerRepliedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ProductReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> StarDistribution { get; set; } = new();
}
