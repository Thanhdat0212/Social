using Application.Common;
using Application.DTOs.Marketplace.Orders;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Orders;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace;

[ApiController]
[Route("api/shop/orders")]
[Authorize]
public class ShopOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IExcelOrderImportService _excelOrderImportService;
    private readonly ICurrentUserService _currentUserService;

    public ShopOrdersController(
        IOrderService orderService,
        IExcelOrderImportService excelOrderImportService,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _excelOrderImportService = excelOrderImportService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Lấy danh sách đơn hàng thuộc Shop của người bán hiện tại (kèm phân trang, lọc theo trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SellerOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetShopOrdersAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng của Shop (chỉ hiển thị số lượng và mặt hàng, không lộ mã đã giao)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SellerOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetShopOrderByIdAsync(CurrentUserId, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng của shop hoặc bạn không phải chủ sở hữu." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo một đơn hàng bán lẻ thủ công (Quick POS) cho khách ngoài sàn
    /// </summary>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(SellerOrderDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateManualOrder(
        [FromBody] CreateManualOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _excelOrderImportService.CreateManualOrderAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Tải file Excel lên để chạy thử nghiệm và kiểm thử tính hợp lệ (Dry Run Preview)
    /// </summary>
    [HttpPost("import-excel/preview")]
    [ProducesResponseType(typeof(ExcelOrderImportPreviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewExcelImport(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp tin Excel (.xlsx, .xls, .csv) hợp lệ." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _excelOrderImportService.PreviewExcelOrdersAsync(CurrentUserId, stream, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xác nhận thực thi tạo đơn hàng loạt từ dữ liệu Preview
    /// </summary>
    [HttpPost("import-excel/execute")]
    [ProducesResponseType(typeof(ExecuteExcelImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteExcelImport(
        [FromBody] ExecuteExcelImportRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _excelOrderImportService.ExecuteExcelImportAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xuất tệp tin Excel chứa riêng danh sách các dòng bị lỗi kèm giải thích nguyên nhân
    /// </summary>
    [HttpPost("import-excel/export-errors")]
    public async Task<IActionResult> ExportErrorRows(
        [FromBody] List<ExcelOrderPreviewRowDto> errorRows,
        CancellationToken cancellationToken)
    {
        var bytes = await _excelOrderImportService.ExportErrorRowsAsync(CurrentUserId, errorRows, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Don_Hang_Loi_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Tải tệp tin Excel mẫu chuẩn (.xlsx) có kèm các SKU mẫu của Shop
    /// </summary>
    [HttpGet("import-excel/template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var bytes = await _excelOrderImportService.GenerateTemplateAsync(CurrentUserId, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_Don_Hang_Hang_Loat.xlsx");
    }
}
