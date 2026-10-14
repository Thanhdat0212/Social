using Application.DTOs.Marketplace.Audit;
using Application.Interfaces.Marketplace.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin")]
public class AdminAuditController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;

    public AdminAuditController(IReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    /// <summary>
    /// Chạy tiến trình đối soát tài chính toàn hệ thống (Kiểm tra cân đối sổ cái Debit/Credit, khớp số dư Shop, khớp Payouts)
    /// </summary>
    [HttpGet("reconciliation")]
    [ProducesResponseType(typeof(ReconciliationSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunReconciliation(CancellationToken cancellationToken)
    {
        var result = await _reconciliationService.RunReconciliationAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Đồng bộ và sửa lại số dư cache cho một gian hàng từ dữ liệu bất biến của Sổ cái kép (Self-healing)
    /// </summary>
    [HttpPost("fix-balance/{shopId:guid}")]
    [ProducesResponseType(typeof(ShopBalanceAuditDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> FixShopBalance(Guid shopId, CancellationToken cancellationToken)
    {
        var result = await _reconciliationService.ReconcileAndFixShopBalanceAsync(shopId, cancellationToken);
        return Ok(result);
    }
}
