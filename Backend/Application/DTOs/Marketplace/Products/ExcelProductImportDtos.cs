namespace Application.DTOs.Marketplace.Products;

/// <summary>
/// DTO chứa thông tin kết quả kiểm thử (Dry Run) của từng dòng sản phẩm trong tệp Excel
/// </summary>
public class ExcelProductPreviewRowDto
{
    public int RowIndex { get; set; }
    public string? RawProductName { get; set; }
    public string? RawCategoryName { get; set; }
    public string? RawVariantName { get; set; }
    public string? RawSku { get; set; }
    public string? RawPrice { get; set; }
    public string? RawCompareAtPrice { get; set; }
    public string? RawDescription { get; set; }
    public string? RawImageUrl { get; set; }
    public string? RawInitialCodes { get; set; }
    public string? RawPublish { get; set; }

    // Kết quả sau khi phân tích và xác thực
    public string? ProductName { get; set; }
    public string? CategoryName { get; set; }
    public Guid? MatchedCategoryId { get; set; }
    public string? MatchedCategoryName { get; set; }
    public string? VariantName { get; set; }
    public string? Sku { get; set; }
    public long Price { get; set; }
    public long? CompareAtPrice { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> InitialCodes { get; set; } = new();
    public bool PublishImmediate { get; set; }

    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Sản phẩm sau khi đã gom nhóm các dòng cùng tên (hỗ trợ sản phẩm nhiều biến thể)
/// </summary>
public class ExcelProductGroupPreviewDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? MatchedCategoryId { get; set; }
    public string? MatchedCategoryName { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool PublishImmediate { get; set; }
    public bool CanBeCreated { get; set; }
    public List<string> BlockingErrors { get; set; } = new();
    public List<ExcelProductPreviewRowDto> Variants { get; set; } = new();
}

/// <summary>
/// Bảng tổng kết xem trước (Preview) trả về cho người bán kiểm tra trước khi tạo hàng loạt
/// </summary>
public class ExcelProductImportPreviewResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int TotalProductCount { get; set; }
    public int ReadyToCreateProductCount { get; set; }
    public List<ExcelProductGroupPreviewDto> Products { get; set; } = new();
    public List<ExcelProductPreviewRowDto> AllRows { get; set; } = new();
}

/// <summary>
/// Yêu cầu thực thi tạo sản phẩm hàng loạt từ dữ liệu Preview
/// </summary>
public class ExecuteProductImportRequestDto
{
    /// <summary>
    /// Nếu true, chỉ tạo các sản phẩm hợp lệ và bỏ qua sản phẩm có dòng lỗi.
    /// Nếu false, hủy bỏ nếu có bất kỳ lỗi nào.
    /// </summary>
    public bool SkipInvalidProducts { get; set; } = true;

    /// <summary>
    /// Danh sách sản phẩm cần tạo (lấy từ kết quả Preview)
    /// </summary>
    public List<ExcelProductGroupPreviewDto> Products { get; set; } = new();
}

/// <summary>
/// Kết quả sau khi thực thi nhập sản phẩm hàng loạt
/// </summary>
public class ExecuteProductImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> CreatedProductNames { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
