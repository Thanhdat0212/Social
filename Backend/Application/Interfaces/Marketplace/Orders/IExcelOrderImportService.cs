using Application.DTOs.Marketplace.Orders;

namespace Application.Interfaces.Marketplace.Orders;

public interface IExcelOrderImportService
{
    /// <summary>
    /// Chạy phân tích cú pháp file Excel và kiểm thử 2 pha (Dry Run Preview)
    /// Áp dụng thuật toán Khớp cột thông minh, Gom nhóm đơn và Khớp SKU mờ.
    /// </summary>
    Task<ExcelOrderImportPreviewResultDto> PreviewExcelOrdersAsync(
        Guid sellerUserId,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi tạo đơn hàng loạt cho các đơn hợp lệ đã xem trước
    /// Tự động khóa và rút kho số (FOR UPDATE SKIP LOCKED) và chuyển mã sang Delivered.
    /// </summary>
    Task<ExecuteExcelImportResultDto> ExecuteExcelImportAsync(
        Guid sellerUserId,
        ExecuteExcelImportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất ngược tệp tin Excel chỉ chứa riêng các dòng lỗi kèm cột nguyên nhân lỗi vi sai
    /// </summary>
    Task<byte[]> ExportErrorRowsAsync(
        Guid sellerUserId,
        List<ExcelOrderPreviewRowDto> errorRows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh tệp tin Excel mẫu chuẩn (.xlsx) có kèm danh sách SKU tham khảo của Shop
    /// </summary>
    Task<byte[]> GenerateTemplateAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo một đơn hàng bán lẻ thủ công đơn lẻ (Quick POS) cho người bán
    /// </summary>
    Task<SellerOrderDetailDto> CreateManualOrderAsync(
        Guid sellerUserId,
        CreateManualOrderRequest request,
        CancellationToken cancellationToken = default);
}
