using Domain.Entities;
using Domain.Entities.Marketplace.Products;

namespace Domain.Entities.Marketplace;

/// <summary>
/// Bảng liên kết nhiều - nhiều giữa Bài viết trên mạng xã hội và Sản phẩm số trên Marketplace (Social Commerce)
/// </summary>
public class PostProduct
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
