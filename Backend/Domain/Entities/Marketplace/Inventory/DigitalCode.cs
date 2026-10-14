using Domain.Common;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Inventory;

/// <summary>
/// Thực thể Mã số / Bản ghi tài khoản trong kho hàng số bảo mật (DigitalCode)
/// </summary>
public class DigitalCode : BaseEntity
{
    public Guid ShopId { get; private set; }
    public Guid VariantId { get; private set; }

    /// <summary>
    /// Giá trị đã được mã hóa AES-GCM an toàn, khóa lưu ngoài cơ sở dữ liệu
    /// </summary>
    public string ValueEncrypted { get; private set; } = string.Empty;

    /// <summary>
    /// Giá trị băm SHA-256 kèm Salt dùng để kiểm tra chống nạp trùng mã (Unique Constraint) mà không cần giải mã
    /// </summary>
    public string ValueHash { get; private set; } = string.Empty;

    /// <summary>
    /// Chuỗi hiển thị che giấu (VD: "***-XYZ1") để Seller nhận diện mã trong kho mà không lộ dữ liệu nhạy cảm
    /// </summary>
    public string MaskedValue { get; private set; } = string.Empty;

    public DigitalCodeStatus Status { get; private set; } = DigitalCodeStatus.Available;

    /// <summary>
    /// ID phiên Checkout đang tạm giữ mã này trong thời gian thanh toán
    /// </summary>
    public Guid? ReservedByCheckoutId { get; private set; }

    /// <summary>
    /// ID dòng đơn hàng (OrderItem) mà mã này đã được xuất giao cho người mua
    /// </summary>
    public Guid? OrderItemId { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    // Navigation
    public Shop? Shop { get; private set; }
    public ProductVariant? Variant { get; private set; }

    protected DigitalCode() { }

    public DigitalCode(
        Guid shopId,
        Guid variantId,
        string valueEncrypted,
        string valueHash,
        string maskedValue)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (variantId == Guid.Empty) throw new ArgumentException("VariantId không hợp lệ.", nameof(variantId));
        if (string.IsNullOrWhiteSpace(valueEncrypted)) throw new ArgumentException("ValueEncrypted không được để trống.", nameof(valueEncrypted));
        if (string.IsNullOrWhiteSpace(valueHash)) throw new ArgumentException("ValueHash không được để trống.", nameof(valueHash));

        ShopId = shopId;
        VariantId = variantId;
        ValueEncrypted = valueEncrypted.Trim();
        ValueHash = valueHash.Trim();
        MaskedValue = string.IsNullOrWhiteSpace(maskedValue) ? "***" : maskedValue.Trim();
        Status = DigitalCodeStatus.Available;
    }

    /// <summary>
    /// Giữ chỗ mã cho phiên thanh toán Checkout (chống bán trùng)
    /// </summary>
    public void MarkReserved(Guid checkoutId)
    {
        if (Status != DigitalCodeStatus.Available)
        {
            throw new InvalidOperationException($"Không thể giữ chỗ mã đang ở trạng thái '{Status}'.");
        }
        if (checkoutId == Guid.Empty) throw new ArgumentException("CheckoutId không hợp lệ.", nameof(checkoutId));

        Status = DigitalCodeStatus.Reserved;
        ReservedByCheckoutId = checkoutId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Giải phóng mã trả về trạng thái Available khi phiên Checkout bị hủy hoặc hết hạn
    /// </summary>
    public void ReleaseReservation()
    {
        if (Status != DigitalCodeStatus.Reserved)
        {
            throw new InvalidOperationException($"Chỉ có thể giải phóng mã đang ở trạng thái Reserved.");
        }

        Status = DigitalCodeStatus.Available;
        ReservedByCheckoutId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu đã giao mã thành công cho đơn hàng
    /// </summary>
    public void MarkDelivered(Guid orderItemId)
    {
        if (Status != DigitalCodeStatus.Reserved && Status != DigitalCodeStatus.Available)
        {
            throw new InvalidOperationException($"Không thể giao mã ở trạng thái '{Status}'.");
        }
        if (orderItemId == Guid.Empty) throw new ArgumentException("OrderItemId không hợp lệ.", nameof(orderItemId));

        Status = DigitalCodeStatus.Delivered;
        OrderItemId = orderItemId;
        DeliveredAtUtc = DateTime.UtcNow;
        ReservedByCheckoutId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu mã bị lỗi / hỏng / không hợp lệ
    /// </summary>
    public void MarkInvalid()
    {
        if (Status == DigitalCodeStatus.Delivered)
        {
            throw new InvalidOperationException("Không thể đánh dấu Invalid đối với mã đã giao cho đơn hàng thành công.");
        }

        Status = DigitalCodeStatus.Invalid;
        ReservedByCheckoutId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
