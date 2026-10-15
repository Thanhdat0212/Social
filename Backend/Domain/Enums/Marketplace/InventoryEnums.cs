namespace Domain.Enums;

/// <summary>
/// Trạng thái tồn kho của mã số / tài khoản trong kho hàng số (CodePool)
/// </summary>
public enum DigitalCodeStatus
{
    /// <summary>
    /// Sẵn sàng bán trong kho
    /// </summary>
    Available = 1,

    /// <summary>
    /// Đang được giữ chỗ trong một phiên thanh toán Checkout (chờ tiền về)
    /// </summary>
    Reserved = 2,

    /// <summary>
    /// Đã giao thành công cho khách hàng sau khi thanh toán xong
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Mã bị lỗi, bị thu hồi hoặc đánh dấu không hợp lệ bởi người bán/hệ thống
    /// </summary>
    Invalid = 4
}
