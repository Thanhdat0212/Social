namespace Domain.Enums;

/// <summary>
/// Trạng thái phiên thanh toán Checkout
/// </summary>
public enum CheckoutStatus
{
    /// <summary>
    /// Đang mở, đang chờ người mua thanh toán (hết hạn sau 15 phút)
    /// </summary>
    Open = 1,

    /// <summary>
    /// Đã thanh toán thành công, đơn hàng đã được khởi tạo và giao mã
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Đã hết hạn thời gian thanh toán (mã giữ chỗ đã được giải phóng lại kho)
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Thanh toán thất bại hoặc người mua chủ động hủy
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Tiền về SAU KHI phiên đã hết hạn và kho không còn đủ mã để giao.
    /// Không sinh đơn hàng; khoản tiền được chuyển sang RefundPayable chờ hoàn trả người mua.
    /// </summary>
    PaidAfterExpiry = 5
}

/// <summary>
/// Trạng thái đơn hàng của từng Shop (ShopOrder)
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Đã giao hàng số thành công cho người mua ngay sau khi thanh toán
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Đã hoàn tất (sau khi hết thời hạn khiếu nại 48h mà không có tranh chấp)
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Đơn hàng bị hủy / hoàn tiền toàn bộ
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Đối tượng thực hiện hành động trên đơn hàng
/// </summary>
public enum OrderActorType
{
    System = 1,
    Buyer = 2,
    Seller = 3,
    Admin = 4
}

/// <summary>
/// Trạng thái xuất giao hàng số (Fulfillment)
/// </summary>
public enum FulfillmentStatus
{
    Fulfilled = 1,
    Failed = 2
}

/// <summary>
/// Nguồn gốc phát sinh đơn hàng
/// </summary>
public enum OrderSource
{
    /// <summary>
    /// Đơn hàng trực tuyến từ sàn thương mại điện tử (PayOS VietQR / Checkout tự động)
    /// </summary>
    Marketplace = 1,

    /// <summary>
    /// Đơn hàng tạo thủ công tại quầy / bán trực tiếp (Seller Manual Order)
    /// </summary>
    ManualEntry = 2,

    /// <summary>
    /// Đơn hàng nhập hàng loạt từ tệp tin Excel / CSV (Batch Excel Import)
    /// </summary>
    ExcelImport = 3
}

