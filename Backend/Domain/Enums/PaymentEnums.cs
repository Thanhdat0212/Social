namespace Domain.Enums;

public enum PaymentStatus
{
    Initiated = 1,
    Succeeded = 2,
    Failed = 3,
    Expired = 4,
    Refunded = 5
}

public enum PaymentProvider
{
    Mock = 1,
    PayOs = 2,
    SePay = 3,
    VietQr = 4
}

public enum LedgerAccountType
{
    GatewayClearing = 1,      // Tiền trung gian cổng thanh toán đang giữ
    UnallocatedPayments = 2,  // Tiền thanh toán đã vào hệ thống nhưng chưa phân bổ đơn hàng
    ShopPending = 3,          // Tiền tạm giữ của Shop (trong thời gian khiếu nại 48h)
    ShopAvailable = 4,        // Tiền khả dụng của Shop (có thể yêu cầu rút)
    PayoutPending = 5,        // Tiền đang xử lý rút ra ngân hàng
    BankClearing = 6,         // Tài khoản đối ứng chuyển khoản ngân hàng
    PlatformRevenue = 7,      // Doanh thu phí hoa hồng của sàn
    PlatformExpense = 8,      // Chi phí khuyến mãi / hỗ trợ của sàn
    RefundPayable = 9         // Khoản phải hoàn trả cho người mua
}

public enum LedgerTransactionType
{
    PaymentReceived = 1,      // Nhận thanh toán từ cổng (Nợ GatewayClearing / Có UnallocatedPayments)
    OrderSettlement = 2,      // Phân bổ đơn hàng (Nợ UnallocatedPayments / Có ShopPending + PlatformRevenue)
    FundRelease = 3,          // Giải phóng tiền hết hạn khiếu nại (Nợ ShopPending / Có ShopAvailable)
    PayoutRequested = 4,      // Shop yêu cầu rút tiền (Nợ ShopAvailable / Có PayoutPending)
    PayoutCompleted = 5,      // Rút tiền thành công qua ngân hàng (Nợ PayoutPending / Có BankClearing)
    PayoutFailed = 6,         // Rút tiền thất bại, hoàn tiền khả dụng (Nợ PayoutPending / Có ShopAvailable)
    RefundProcessed = 7       // Hoàn tiền cho đơn hàng (Nợ ShopPending/Available / Có RefundPayable)
}
