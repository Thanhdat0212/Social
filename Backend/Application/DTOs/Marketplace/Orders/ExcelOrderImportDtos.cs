namespace Application.DTOs.Marketplace.Orders;

/// <summary>
/// DTO chứa thông tin kết quả kiểm thử (Dry Run) của từng dòng trong tệp Excel
/// </summary>
public class ExcelOrderPreviewRowDto
{
    public int RowIndex { get; set; }
    public string? RawOrderCode { get; set; }
    public string? RawSkuOrProduct { get; set; }
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? Note { get; set; }

    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    // Thông tin sản phẩm/biến thể sau khi chạy thuật toán khớp mờ (Fuzzy SKU Matching)
    public Guid? MatchedVariantId { get; set; }
    public Guid? MatchedProductId { get; set; }
    public string? MatchedProductName { get; set; }
    public string? MatchedVariantLabel { get; set; }
    public string? MatchedSku { get; set; }
    public int AvailableStock { get; set; }
    public double MatchConfidence { get; set; } = 100.0;
}

/// <summary>
/// Đơn hàng sau khi đã gom nhóm các dòng cùng mã đơn hoặc cùng thông tin khách hàng
/// </summary>
public class ExcelOrderGroupPreviewDto
{
    public string OrderGroupKey { get; set; } = string.Empty;
    public string? OrderNo { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? Note { get; set; }
    public long TotalAmount { get; set; }
    public bool CanBeCreated { get; set; }
    public List<string> BlockingErrors { get; set; } = new();
    public List<ExcelOrderPreviewRowDto> Items { get; set; } = new();
}

/// <summary>
/// Bảng tổng kết xem trước (Preview) trả về cho Seller kiểm tra trước khi tạo đơn
/// </summary>
public class ExcelOrderImportPreviewResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int TotalOrderCount { get; set; }
    public int ReadyToCreateOrderCount { get; set; }
    public List<ExcelOrderGroupPreviewDto> Orders { get; set; } = new();
    public List<ExcelOrderPreviewRowDto> AllRows { get; set; } = new();
}

/// <summary>
/// Yêu cầu thực thi tạo đơn hàng loạt từ dữ liệu Preview
/// </summary>
public class ExecuteExcelImportRequestDto
{
    /// <summary>
    /// Nếu true, chỉ tạo các đơn hợp lệ và bỏ qua các đơn có dòng lỗi.
    /// Nếu false, hủy bỏ nếu có bất kỳ lỗi nào.
    /// </summary>
    public bool SkipInvalidOrders { get; set; } = true;

    /// <summary>
    /// Danh sách các nhóm đơn hàng cần tạo (lấy từ kết quả Preview)
    /// </summary>
    public List<ExcelOrderGroupPreviewDto> Orders { get; set; } = new();
}

/// <summary>
/// Kết quả sau khi thực thi nhập đơn hàng loạt
/// </summary>
public class ExecuteExcelImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> CreatedOrderNos { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Yêu cầu tạo một đơn bán lẻ thủ công (Quick POS) cho khách ngoài sàn
/// </summary>
public class CreateManualOrderRequest
{
    public Guid VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public long? CustomUnitPrice { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Note { get; set; }
}
