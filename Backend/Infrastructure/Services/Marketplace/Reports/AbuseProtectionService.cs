using Application.DTOs.Marketplace.Products;
using Application.DTOs.Marketplace.Reports;
using Application.DTOs.Marketplace.Shops;
using Application.Interfaces.Marketplace.Products;
using Application.Interfaces.Marketplace.Reports;
using Application.Interfaces.Marketplace.Shops;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Reports;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Marketplace.Reports;

public class AbuseProtectionService : IAbuseProtectionService
{
    private readonly SocialDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminProductService _adminProductService;
    private readonly IAdminShopService _adminShopService;
    private readonly ILogger<AbuseProtectionService> _logger;

    public AbuseProtectionService(
        SocialDbContext dbContext,
        IUnitOfWork unitOfWork,
        IAdminProductService adminProductService,
        IAdminShopService adminShopService,
        ILogger<AbuseProtectionService> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _adminProductService = adminProductService;
        _adminShopService = adminShopService;
        _logger = logger;
    }

    public async Task<ProductReportDto> ReportProductAsync(
        Guid reporterUserId,
        Guid productId,
        CreateProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Shop)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            throw new KeyNotFoundException("Không tìm thấy sản phẩm cần báo cáo.");

        if (product.Shop?.OwnerUserId == reporterUserId)
            throw new InvalidOperationException("Bạn không thể tự báo cáo sản phẩm của gian hàng mình.");

        // Chống spam: 1 User chỉ có tối đa 1 báo cáo Pending trên 1 sản phẩm
        var hasPendingReport = await _dbContext.ProductReports
            .AnyAsync(r => r.ReporterUserId == reporterUserId && r.ProductId == productId && r.Status == ReportStatus.Pending, cancellationToken);

        if (hasPendingReport)
            throw new InvalidOperationException("Bạn đã gửi một báo cáo cho sản phẩm này và đang chờ quản trị viên xử lý.");

        var report = new ProductReport(
            reporterUserId,
            productId,
            product.ShopId,
            request.Reason,
            request.Description,
            request.EvidenceUrl);

        await _dbContext.ProductReports.AddAsync(report, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã gửi báo cáo vi phạm #{ReportId} cho sản phẩm {ProductId} (Lý do: {Reason}).",
            reporterUserId, report.Id, productId, request.Reason);

        return MapToReportDto(report, product.Name, product.Slug, product.Shop?.Name ?? string.Empty);
    }

    public async Task<PagedResult<ProductReportDto>> GetAdminReportsAsync(
        ReportStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductReports
            .Include(r => r.Product)
            .Include(r => r.Shop)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(r => MapToReportDto(
            r,
            r.Product?.Name ?? string.Empty,
            r.Product?.Slug ?? string.Empty,
            r.Shop?.Name ?? string.Empty)).ToList();

        return new PagedResult<ProductReportDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductReportDto> GetReportDetailAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.ProductReports
            .Include(r => r.Product)
            .Include(r => r.Shop)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report == null)
            throw new KeyNotFoundException("Không tìm thấy báo cáo vi phạm.");

        return MapToReportDto(
            report,
            report.Product?.Name ?? string.Empty,
            report.Product?.Slug ?? string.Empty,
            report.Shop?.Name ?? string.Empty);
    }

    public async Task<ProductReportDto> ResolveReportAsync(
        Guid adminId,
        Guid reportId,
        ResolveProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.ProductReports
            .Include(r => r.Product)
            .Include(r => r.Shop)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report == null)
            throw new KeyNotFoundException("Không tìm thấy báo cáo vi phạm.");

        report.Resolve(adminId, request.ResolutionStatus, request.AdminNote);
        _dbContext.ProductReports.Update(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Nếu xác nhận có vi phạm và yêu cầu gỡ sản phẩm
        if (request.ResolutionStatus == ReportStatus.ActionTaken && request.TakedownProduct)
        {
            try
            {
                await _adminProductService.TakedownAsync(adminId, report.ProductId, $"Gỡ theo báo cáo vi phạm #{report.Id}: {request.AdminNote}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sản phẩm {ProductId} có thể đã bị gỡ trước đó.", report.ProductId);
            }
        }

        // Nếu xác nhận có vi phạm và yêu cầu chế tài xử phạt Shop
        if (request.ResolutionStatus == ReportStatus.ActionTaken && request.ApplySanctionToShop)
        {
            try
            {
                await _adminShopService.AddSanctionAsync(
                    report.ShopId,
                    new CreateSanctionRequestDto
                    {
                        Type = ShopSanctionType.Warning,
                        Reason = $"Xử phạt theo báo cáo vi phạm #{report.Id}: {request.AdminNote}"
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi áp dụng chế tài lên gian hàng {ShopId}.", report.ShopId);
            }
        }

        _logger.LogInformation("Admin {AdminId} đã phân xử báo cáo #{ReportId}: Kết quả={Status}.",
            adminId, report.Id, request.ResolutionStatus);

        return MapToReportDto(
            report,
            report.Product?.Name ?? string.Empty,
            report.Product?.Slug ?? string.Empty,
            report.Shop?.Name ?? string.Empty);
    }

    public async Task<ShopPerformanceDto> GetShopPerformanceAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.Id == shopId, cancellationToken);
        if (shop == null)
            throw new KeyNotFoundException("Không tìm thấy gian hàng.");

        // 1. Đếm tổng số đơn hàng đã phát sinh
        var totalOrders = await _dbContext.ShopOrders
            .CountAsync(o => o.ShopId == shopId && o.Status != OrderStatus.Cancelled, cancellationToken);

        var completedOrders = await _dbContext.ShopOrders
            .CountAsync(o => o.ShopId == shopId && o.Status == OrderStatus.Completed, cancellationToken);

        // 2. Đếm số khiếu nại phát sinh
        var totalComplaints = await _dbContext.Complaints
            .CountAsync(c => c.ShopId == shopId, cancellationToken);

        // 3. Đếm số khiếu nại hợp lệ (bị Admin phân xử hoàn tiền hoặc shop chấp thuận đổi hàng)
        var validComplaints = await _dbContext.Complaints
            .CountAsync(c => c.ShopId == shopId && (
                c.Status == ComplaintStatus.ResolvedRefund ||
                c.Status == ComplaintStatus.ResolvedReplacement ||
                c.Status == ComplaintStatus.ShopAccepted), cancellationToken);

        // 4. Tính Defect Rate (%)
        double defectRate = totalOrders > 0
            ? Math.Round(((double)validComplaints / totalOrders) * 100.0, 2)
            : 0.0;

        bool isHighRisk = totalOrders >= 10 && defectRate > 5.0;

        string tier = defectRate <= 1.0 ? "Excellent" :
                      defectRate <= 3.0 ? "Good" :
                      defectRate <= 5.0 ? "Fair" : "HighRisk";

        return new ShopPerformanceDto
        {
            ShopId = shop.Id,
            ShopName = shop.Name,
            TotalOrders = totalOrders,
            CompletedOrders = completedOrders,
            TotalComplaints = totalComplaints,
            ValidComplaints = validComplaints,
            DefectRate = defectRate,
            IsHighRisk = isHighRisk,
            PerformanceTier = tier
        };
    }

    public async Task<ShopPerformanceDto> GetMyShopPerformanceAsync(Guid sellerUserId, CancellationToken cancellationToken = default)
    {
        var shop = await _unitOfWork.Shops.GetByOwnerUserIdAsync(sellerUserId, cancellationToken);
        if (shop == null)
            throw new InvalidOperationException("Bạn chưa mở gian hàng trên hệ thống.");

        return await GetShopPerformanceAsync(shop.Id, cancellationToken);
    }

    private static ProductReportDto MapToReportDto(
        ProductReport report,
        string productName,
        string productSlug,
        string shopName)
    {
        return new ProductReportDto
        {
            Id = report.Id,
            ReporterUserId = report.ReporterUserId,
            ProductId = report.ProductId,
            ProductName = productName,
            ProductSlug = productSlug,
            ShopId = report.ShopId,
            ShopName = shopName,
            Reason = report.Reason,
            Description = report.Description,
            EvidenceUrl = report.EvidenceUrl,
            Status = report.Status,
            AdminResolutionNote = report.AdminResolutionNote,
            CreatedAtUtc = report.CreatedAtUtc,
            ResolvedAtUtc = report.ResolvedAtUtc
        };
    }
}
