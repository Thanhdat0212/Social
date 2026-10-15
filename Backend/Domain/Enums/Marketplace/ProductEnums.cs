namespace Domain.Enums;

/// <summary>
/// Trạng thái vòng đời của Sản phẩm:
/// Draft -> (PendingReview nếu danh mục yêu cầu) -> Listed <-> Hidden -> Archived
/// </summary>
public enum ProductStatus
{
    Draft = 1,
    PendingReview = 2,
    Listed = 3,
    Hidden = 4,
    Archived = 5
}

/// <summary>
/// Hình thức giao hàng số của biến thể sản phẩm
/// </summary>
public enum DeliveryType
{
    /// <summary>
    /// Kho mã/key bản quyền hoặc tài khoản (CodePool)
    /// </summary>
    CodePool = 1,

    /// <summary>
    /// File tải về (Tài liệu, source code qua link chữ ký)
    /// </summary>
    File = 2,

    /// <summary>
    /// Khóa học (thực chất là chia sẻ Account credentials/license access từ CodePool)
    /// </summary>
    Course = 3,

    /// <summary>
    /// Giao thủ công (dự phòng)
    /// </summary>
    Manual = 4
}
