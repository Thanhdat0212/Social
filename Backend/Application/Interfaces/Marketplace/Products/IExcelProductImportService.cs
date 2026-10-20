using Application.DTOs.Marketplace.Products;

namespace Application.Interfaces.Marketplace.Products;

public interface IExcelProductImportService
{
    /// <summary>
    /// Chạy phân tích cú pháp file Excel và kiểm thử dữ liệu 2 pha (Dry Run Preview)
    /// Nhận diện cột thông minh, khớp danh mục, validate giá/SKU và gom nhóm biến thể.
    /// </summary>
    Task<ExcelProductImportPreviewResultDto> PreviewExcelProductsAsync(
        Guid sellerUserId,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi tạo sản phẩm hàng loạt từ danh sách sản phẩm hợp lệ đã xem trước
    /// Tự động sinh Slug, SKU duy nhất, nạp ảnh và kho mã số khởi tạo (nếu có).
    /// </summary>
    Task<ExecuteProductImportResultDto> ExecuteProductImportAsync(
        Guid sellerUserId,
        ExecuteProductImportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất ngược tệp tin Excel chỉ chứa riêng các dòng lỗi kèm cột giải thích nguyên nhân lỗi
    /// </summary>
    Task<byte[]> ExportErrorRowsAsync(
        Guid sellerUserId,
        List<ExcelProductPreviewRowDto> errorRows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo file Excel mẫu chuẩn (.xlsx) kèm dữ liệu ví dụ và danh mục có sẵn của sàn
    /// </summary>
    Task<byte[]> GenerateTemplateAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default);
}
