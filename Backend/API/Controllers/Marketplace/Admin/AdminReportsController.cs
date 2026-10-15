using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.DTOs.Marketplace.Reports;
using Application.Interfaces.Marketplace.Reports;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IAbuseProtectionService _abuseProtectionService;
    private readonly ICurrentUserService _currentUserService;

    public AdminReportsController(
        IAbuseProtectionService abuseProtectionService,
        ICurrentUserService currentUserService)
    {
        _abuseProtectionService = abuseProtectionService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Quản trị viên chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách hàng đợi các báo cáo vi phạm sản phẩm cần xử lý
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(PagedResult<ProductReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] ReportStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await _abuseProtectionService.GetAdminReportsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một báo cáo vi phạm sản phẩm
    /// </summary>
    [HttpGet("reports/{id:guid}")]
    [ProducesResponseType(typeof(ProductReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _abuseProtectionService.GetReportDetailAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Quản trị viên xử lý báo cáo vi phạm (Hạ gỡ sản phẩm, Áp dụng chế tài cảnh cáo shop, hoặc Bác bỏ báo cáo)
    /// </summary>
    [HttpPost("reports/{id:guid}/resolve")]
    [ProducesResponseType(typeof(ProductReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveReport(
        Guid id,
        [FromBody] ResolveProductReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _abuseProtectionService.ResolveReportAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Quản trị viên tra cứu chỉ số hiệu suất & Tỷ lệ đơn lỗi (Defect Rate %) của một gian hàng
    /// </summary>
    [HttpGet("shops/{shopId:guid}/performance")]
    [ProducesResponseType(typeof(ShopPerformanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopPerformance(Guid shopId, CancellationToken cancellationToken)
    {
        var result = await _abuseProtectionService.GetShopPerformanceAsync(shopId, cancellationToken);
        return Ok(result);
    }
}
