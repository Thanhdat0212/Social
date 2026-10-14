using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Products;

namespace Domain.Entities.Marketplace.Orders;

/// <summary>
/// Thực thể Giỏ hàng của người dùng (Cart)
/// Quy tắc: Giỏ hàng không lưu giá cố định và không giữ hàng trong kho. Giá được tính động từ DB.
/// </summary>
public class Cart : BaseEntity
{
    public Guid UserId { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    protected Cart() { }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId không hợp lệ.", nameof(userId));
        UserId = userId;
    }
}

/// <summary>
/// Dòng hàng trong giỏ hàng
/// </summary>
public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; } = 1;

    // Navigation
    public Cart? Cart { get; private set; }
    public ProductVariant? Variant { get; private set; }

    protected CartItem() { }

    public CartItem(Guid cartId, Guid variantId, int quantity)
    {
        if (cartId == Guid.Empty) throw new ArgumentException("CartId không hợp lệ.", nameof(cartId));
        if (variantId == Guid.Empty) throw new ArgumentException("VariantId không hợp lệ.", nameof(variantId));
        if (quantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(quantity));

        CartId = cartId;
        VariantId = variantId;
        Quantity = quantity;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(quantity));
        Quantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncreaseQuantity(int delta = 1)
    {
        Quantity += delta;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
