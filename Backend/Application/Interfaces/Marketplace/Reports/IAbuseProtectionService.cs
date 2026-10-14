using Application.DTOs.Marketplace.Products;
using Application.DTOs.Marketplace.Reports;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Reports;

public interface IAbuseProtectionService
{
    // Buyer / User
    Task<ProductReportDto> ReportProductAsync(
        Guid reporterUserId,
        Guid productId,
        CreateProductReportRequest request,
        CancellationToken cancellationToken = default);

    // Admin
    Task<PagedResult<ProductReportDto>> GetAdminReportsAsync(
        ReportStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProductReportDto> GetReportDetailAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ProductReportDto> ResolveReportAsync(
        Guid adminId,
        Guid reportId,
        ResolveProductReportRequest request,
        CancellationToken cancellationToken = default);

    // Shop Performance & Defect Rate
    Task<ShopPerformanceDto> GetShopPerformanceAsync(
        Guid shopId,
        CancellationToken cancellationToken = default);

    Task<ShopPerformanceDto> GetMyShopPerformanceAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default);
}
