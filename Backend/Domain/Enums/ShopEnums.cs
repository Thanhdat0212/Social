namespace Domain.Enums;

/// <summary>
/// Trạng thái xét duyệt gian hàng bởi Admin
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Trạng thái hoạt động do chủ shop tự quyết định (Mở / Tạm đóng cửa)
/// </summary>
public enum OperatingStatus
{
    Open = 0,
    Closed = 1
}

/// <summary>
/// Quyết định của Admin trong phiên duyệt hồ sơ gian hàng
/// </summary>
public enum ShopReviewDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Loại chế tài / hình thức phạt áp dụng lên gian hàng vi phạm
/// </summary>
public enum ShopSanctionType
{
    TemporarySuspension = 0, // Tạm ngưng hoạt động có thời hạn
    PermanentBan = 1,        // Khóa vĩnh viễn
    Warning = 2              // Cảnh cáo (vẫn cho mở cửa)
}

/// <summary>
/// Trạng thái hiệu lực tổng hợp của Shop khi hiển thị (tính toán runtime)
/// </summary>
public enum ShopEffectiveStatus
{
    PendingReview = 0,  // Đang chờ xét duyệt
    Rejected = 1,       // Bị từ chối duyệt (có thể sửa để nộp lại)
    Suspended = 2,      // Đang bị xử phạt / tạm ngưng bởi Admin
    Closed = 3,         // Chủ shop tự tạm đóng cửa
    UserUnavailable = 4,// Tài khoản chủ sở hữu User đang bị khóa
    Active = 5,         // Đang hoạt động bình thường
    Deleted = 6         // Gian hàng đã bị xóa
}
