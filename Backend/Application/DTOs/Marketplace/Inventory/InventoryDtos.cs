using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Marketplace.Inventory;

public class BulkImportCodesRequest
{
    /// <summary>
    /// Chuỗi văn bản dán trực tiếp nhiều dòng (mỗi dòng 1 mã/tài khoản)
    /// </summary>
    public string? RawCodes { get; set; }

    /// <summary>
    /// Hoặc danh sách các mã dưới dạng mảng JSON
    /// </summary>
    public List<string>? Codes { get; set; }
}

public class BulkImportCodesResultDto
{
    public int TotalSubmitted { get; set; }
    public int SuccessfullyAdded { get; set; }
    public int DuplicatesCount { get; set; }
    public int InvalidCount { get; set; }
    public List<string> DuplicateSamples { get; set; } = new();
}

public class DigitalCodeItemDto
{
    public Guid Id { get; set; }
    public string MaskedValue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class InventorySummaryDto
{
    public Guid VariantId { get; set; }
    public int Total { get; set; }
    public int Available { get; set; }
    public int Reserved { get; set; }
    public int Delivered { get; set; }
    public int Invalid { get; set; }
}

public class ConfigureDigitalFileRequest
{
    [Required(ErrorMessage = "Tên tệp tin không được để trống")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn lưu trữ không được để trống")]
    [MaxLength(1000)]
    public string StoragePath { get; set; } = string.Empty;

    [Range(0, long.MaxValue, ErrorMessage = "Dung lượng tệp không hợp lệ")]
    public long FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public int? MaxDownloads { get; set; }
}

public class DigitalFileDetailDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int? MaxDownloads { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
