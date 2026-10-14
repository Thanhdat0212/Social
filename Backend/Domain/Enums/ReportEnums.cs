namespace Domain.Enums;

public enum ProductReportReason
{
    Counterfeit = 1,            // Hàng giả / Hàng nhái
    ScamOrFraud = 2,            // Lừa đảo / Chiếm đoạt
    CopyrightInfringement = 3,  // Vi phạm bản quyền / Sở hữu trí tuệ
    ProhibitedContent = 4,      // Hàng hóa trong danh mục cấm giao dịch
    MisleadingInfo = 5,         // Mô tả sai lệch, gian lận thông tin
    StolenAccountOrData = 6,    // Tài khoản hack / Dữ liệu đánh cắp
    Other = 99                  // Lý do khác
}

public enum ReportStatus
{
    Pending = 1,       // Chờ Admin xem xét
    Reviewed = 2,      // Admin đang xử lý
    ActionTaken = 3,   // Đã xử lý (gỡ sản phẩm hoặc phạt shop)
    Dismissed = 4      // Bác bỏ (báo cáo không hợp lệ / spam)
}
