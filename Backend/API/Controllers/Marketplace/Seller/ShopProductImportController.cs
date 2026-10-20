using Application.Common;
using Application.DTOs.Marketplace.Products;
using Application.Interfaces.Marketplace.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Marketplace.Seller;

[ApiController]
[Route("api/shop/products/import-excel")]
[Authorize]
public class ShopProductImportController : ControllerBase
{
    private readonly IExcelProductImportService _excelProductImportService;
    private readonly ICurrentUserService _currentUserService;

    public ShopProductImportController(
        IExcelProductImportService excelProductImportService,
        ICurrentUserService currentUserService)
    {
        _excelProductImportService = excelProductImportService;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId => _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

    /// <summary>
    /// Tải tệp tin Excel mẫu chuẩn (.xlsx) kèm dữ liệu ví dụ và danh mục có sẵn
    /// </summary>
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var bytes = await _excelProductImportService.GenerateTemplateAsync(CurrentUserId, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_San_Pham_Hang_Loat.xlsx");
    }

    /// <summary>
    /// Tải file Excel lên để chạy thử nghiệm và kiểm thử tính hợp lệ (Dry Run Preview)
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(ExcelProductImportPreviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewExcelImport(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp tin Excel (.xlsx, .xls, .csv) hợp lệ." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _excelProductImportService.PreviewExcelProductsAsync(CurrentUserId, stream, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xác nhận thực thi tạo sản phẩm hàng loạt từ dữ liệu Preview
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ExecuteProductImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteExcelImport(
        [FromBody] ExecuteProductImportRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _excelProductImportService.ExecuteProductImportAsync(CurrentUserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xuất tệp tin Excel chứa riêng danh sách các dòng bị lỗi kèm giải thích nguyên nhân
    /// </summary>
    [HttpPost("export-errors")]
    public async Task<IActionResult> ExportErrorRows(
        [FromBody] List<ExcelProductPreviewRowDto> errorRows,
        CancellationToken cancellationToken)
    {
        var bytes = await _excelProductImportService.ExportErrorRowsAsync(CurrentUserId, errorRows, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"San_Pham_Loi_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }
}
