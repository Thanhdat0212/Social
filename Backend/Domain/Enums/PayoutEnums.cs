namespace Domain.Enums;

public enum PayoutStatus
{
    Requested = 1,   // Shop Owner tạo yêu cầu rút, tiền bị khóa từ Available -> PayoutPending
    Approved = 2,    // Admin duyệt lệnh rút, chuẩn bị giải ngân
    Processing = 3,  // Đang thực hiện chuyển khoản
    Succeeded = 4,   // Tiền đã về tài khoản ngân hàng của chủ shop (Nợ PayoutPending / Có BankClearing)
    Failed = 5,      // Chuyển khoản thất bại rõ ràng (bút toán đảo Nợ PayoutPending / Có ShopAvailable)
    Unknown = 6,     // Mất kết nối / timeout (chờ tra soát, TUYỆT ĐỐI không tự động hoàn tiền)
    Rejected = 7,    // Admin từ chối (bút toán đảo Nợ PayoutPending / Có ShopAvailable)
    Cancelled = 8    // Shop tự hủy khi lệnh còn ở trạng thái Requested
}

public enum BankAccountStatus
{
    Pending = 1,   // Chờ xác minh
    Verified = 2,  // Đã xác thực, sẵn sàng nhận tiền rút
    Disabled = 3   // Đã vô hiệu hóa
}

public enum PayoutActorType
{
    ShopOwner = 1,
    Admin = 2,
    System = 3
}
