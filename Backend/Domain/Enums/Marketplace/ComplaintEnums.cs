namespace Domain.Enums;

public enum ComplaintStatus
{
    Submitted = 1,          // Người mua mới gửi khiếu nại, chờ Shop phản hồi trong 24h
    ShopAccepted = 2,       // Shop chấp thuận (hoàn tiền hoặc đổi mã mới)
    ShopRejected = 3,       // Shop từ chối (chuyển sang Admin phân xử)
    EscalatedToAdmin = 4,   // Quá hạn 24h Shop im lặng hoặc tranh chấp, Admin thụ lý
    ResolvedRefund = 5,     // Đã phán quyết: Hoàn tiền cho người mua
    ResolvedReplacement = 6,// Đã phán quyết: Đổi mã số mới từ kho cho người mua
    Rejected = 7,           // Bị từ chối khiếu nại (bằng chứng không hợp lệ / hàng đúng mô tả)
    Cancelled = 8           // Người mua tự hủy khiếu nại
}

public enum ComplaintReason
{
    InvalidOrUsedCode = 1,       // Mã sai, không hoạt động hoặc đã bị người khác sử dụng trước
    CorruptedFileOrMismatch = 2, // File bị hỏng, nhiễm mã độc hoặc sai hoàn toàn mô tả
    LoginFailed = 3,             // Tài khoản cung cấp sai mật khẩu / bị khóa từ trước
    ServiceNotDelivered = 4,     // Dịch vụ số chưa được kích hoạt như cam kết
    Other = 5                    // Lý do khác
}

public enum RefundBearer
{
    Shop = 1,       // Shop chịu tiền hàng, sàn hoàn lại phí hoa hồng tương ứng
    Platform = 2,   // Sàn chịu toàn bộ (do lỗi hệ thống của sàn)
    Shared = 3      // Cả hai cùng chia sẻ trách nhiệm
}

public enum RefundStatus
{
    Pending = 1,    // Nghĩa vụ hoàn tiền đã được tạo, chờ giải ngân
    Processing = 2, // Đang gọi cổng thanh toán hoàn tiền
    Succeeded = 3,  // Đã hoàn tiền thành công cho khách hàng
    Failed = 4      // Hoàn tiền qua cổng thất bại
}

public enum RiskFlagType
{
    ExcessiveComplaints = 1, // Khiếu nại bất thường nhiều lần
    SuspectedFraud = 2       // Nghi vấn gian lận nhằm chiếm đoạt tài khoản/mã số
}
