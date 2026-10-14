using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Marketplace.Orders;

#region Cart DTOs

public class AddToCartRequest
{
    [Required]
    public Guid VariantId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mua mỗi lần từ 1 đến 100")]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 100, ErrorMessage = "Số lượng mua mỗi lần từ 1 đến 100")]
    public int Quantity { get; set; } = 1;
}

public class CartDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public int TotalItems => Items.Sum(i => i.Quantity);
    public long EstimatedTotal => Items.Where(i => i.IsAvailable).Sum(i => i.LineTotal);
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public long Price => UnitPrice;
    public int Quantity { get; set; }
    public long LineTotal => UnitPrice * Quantity;
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ImageUrl => CoverImageUrl;
    public string VariantName => VariantLabel;
    public int AvailableStock { get; set; }
}

#endregion

#region Checkout DTOs

public class CreateCheckoutRequest
{
    /// <summary>
    /// Danh sách VariantId được chọn thanh toán từ giỏ hàng (nếu null/empty thì thanh toán toàn bộ giỏ)
    /// </summary>
    public List<Guid>? SelectedVariantIds { get; set; }

    /// <summary>
    /// Danh sách items thanh toán gửi trực tiếp từ client
    /// </summary>
    public List<CheckoutItemInput>? Items { get; set; }

    /// <summary>
    /// Mua ngay 1 món bỏ qua giỏ hàng
    /// </summary>
    public BuyNowItemInput? BuyNowItem { get; set; }

    public string? IdempotencyKey { get; set; }
}

public class CheckoutItemInput
{
    [Required]
    public Guid VariantId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mua mỗi lần từ 1 đến 100")]
    public int Quantity { get; set; } = 1;
}

public class BuyNowItemInput
{
    [Required]
    public Guid VariantId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mua mỗi lần từ 1 đến 100")]
    public int Quantity { get; set; } = 1;
}

public class CheckoutDetailDto
{
    public Guid Id { get; set; }
    public string CheckoutNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long ItemsTotal { get; set; }
    public long DiscountTotal { get; set; }
    public long GrandTotal { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public List<CheckoutItemDetailDto> Items { get; set; } = new();
}

public class CheckoutItemDetailDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
}

public class MockPayRequest
{
    /// <summary>
    /// "success" hoặc "fail"
    /// </summary>
    [Required]
    public string Result { get; set; } = "success";
}

public class MockPayResultDto
{
    public Guid CheckoutId { get; set; }
    public string CheckoutStatus { get; set; } = string.Empty;
    public List<Guid> OrderIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả xử lý hoàn tất thanh toán của một Checkout.
/// RefundRequired = true: tiền về sau khi phiên hết hạn và kho đã hết mã -> không có đơn, cần hoàn tiền.
/// </summary>
public sealed record CheckoutPaymentResult(bool RefundRequired, List<Guid> OrderIds);

#endregion

#region Order DTOs

public class BuyerOrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Total { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? ComplaintWindowEndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<BuyerOrderItemDto> Items { get; set; } = new();
}

public class BuyerOrderItemDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
    public string DeliveryType { get; set; } = string.Empty;

    /// <summary>
    /// Các mã bản quyền / tài khoản đã được giải mã AES-GCM dành riêng cho người mua
    /// </summary>
    public List<string> DeliveredCodes { get; set; } = new();

    /// <summary>
    /// Tệp tin tải về (nếu loại File)
    /// </summary>
    public string? DownloadFileName { get; set; }
    public string? DownloadStoragePath { get; set; }
    public string? SignedDownloadUrl { get; set; }
    public int DownloadCount { get; set; } = 0;
    public int? MaxDownloads { get; set; }
}

public class SignedDownloadUrlDto
{
    public Guid OrderItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int DownloadCount { get; set; }
    public int? MaxDownloads { get; set; }
}

public class SellerOrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid BuyerUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Total { get; set; }
    public long ShopGross { get; set; }
    public long CommissionTotal { get; set; }
    public long ShopNet { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<SellerOrderItemDto> Items { get; set; } = new();
}

public class SellerOrderItemDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
    public long ShopNet { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
}

#endregion
