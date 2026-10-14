using Application.Common;
using Application.DTOs.Marketplace.Reports;
using Application.Interfaces.Marketplace.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers.Marketplace;

[ApiController]
[Authorize]
[EnableRateLimiting("report-policy")]
public class ProductReportsController : ControllerBase
{
    private readonly IAbuseProtectionService _abuseProtectionService;
    private readonly ICurrentUserService _currentUserService;

    public ProductReportsController(
        IAbuseProtectionService abuseProtectionService,
        ICurrentUserService currentUserService)
    {
        _abuseProtectionService = abuseProtectionService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Người mua gửi báo cáo vi phạm đối với một sản phẩm (Hàng giả, Lừa đảo, Vi phạm bản quyền, Mô tả sai lệch...)
    /// </summary>
    [HttpPost("api/products/{id:guid}/report")]
    [ProducesResponseType(typeof(ProductReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReportProduct(
        Guid id,
        [FromBody] CreateProductReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _abuseProtectionService.ReportProductAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Chủ gian hàng xem chỉ số hiệu suất bán hàng và Tỷ lệ đơn lỗi (Defect Rate %) của gian hàng mình
    /// </summary>
    [HttpGet("api/shop/performance")]
    [ProducesResponseType(typeof(ShopPerformanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyShopPerformance(CancellationToken cancellationToken)
    {
        var result = await _abuseProtectionService.GetMyShopPerformanceAsync(CurrentUserId, cancellationToken);
        return Ok(result);
    }
}
