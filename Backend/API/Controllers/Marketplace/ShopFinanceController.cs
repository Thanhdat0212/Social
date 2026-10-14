using Application.Common;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Ledger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/finance")]
[Authorize]
public class ShopFinanceController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly ICurrentUserService _currentUserService;

    public ShopFinanceController(
        ILedgerService ledgerService,
        ICurrentUserService currentUserService)
    {
        _ledgerService = ledgerService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Xem thông tin số dư tài chính của Shop (Tiền tạm giữ khiếu nại, Tiền khả dụng rút, Tiền đang chờ rút, Doanh thu lũy kế)
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(ShopBalanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        var result = await _ledgerService.GetShopBalanceAsync(CurrentUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xem bảng sao kê lịch sử biến động số dư / bút toán sổ cái của Shop
    /// </summary>
    [HttpGet("statement")]
    [ProducesResponseType(typeof(PagedResult<LedgerStatementItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatement(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _ledgerService.GetShopStatementAsync(CurrentUserId, fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
