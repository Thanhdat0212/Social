using Application.DTOs.Marketplace.Inventory;
using Application.DTOs.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Inventory;

public interface IInventoryService
{
    /// <summary>
    /// Nạp mã số / tài khoản hàng loạt vào kho hàng số (Bulk Import)
    /// </summary>
    Task<BulkImportCodesResultDto> BulkImportCodesAsync(
        Guid userId,
        Guid variantId,
        BulkImportCodesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xem danh sách mã trong kho của biến thể (Mã được che giấu ***-XXXX bảo mật)
    /// </summary>
    Task<PagedResult<DigitalCodeItemDto>> GetCodesAsync(
        Guid userId,
        Guid variantId,
        DigitalCodeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thống kê chi tiết số lượng tồn kho theo từng trạng thái (Available, Reserved, Delivered, Invalid)
    /// </summary>
    Task<InventorySummaryDto> GetInventorySummaryAsync(
        Guid userId,
        Guid variantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu một mã bị hỏng / không hợp lệ
    /// </summary>
    Task InvalidateCodeAsync(
        Guid userId,
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một mã khỏi kho (chỉ áp dụng với mã còn ở trạng thái Available)
    /// </summary>
    Task DeleteCodeAsync(
        Guid userId,
        Guid variantId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thiết lập tệp tin số cho biến thể loại File (source code, tài liệu e-book)
    /// </summary>
    Task<DigitalFileDetailDto> ConfigureDigitalFileAsync(
        Guid userId,
        Guid variantId,
        ConfigureDigitalFileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin tệp tin số của biến thể
    /// </summary>
    Task<DigitalFileDetailDto?> GetDigitalFileAsync(
        Guid userId,
        Guid variantId,
        CancellationToken cancellationToken = default);
}
