using Domain.Common;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;

namespace Domain.Entities.Marketplace.Inventory;

/// <summary>
/// Thực thể Tệp tin số được bán kèm theo biến thể (DigitalFile - e.g. source code, tài liệu e-book)
/// </summary>
public class DigitalFile : BaseEntity
{
    public Guid ShopId { get; private set; }
    public Guid VariantId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; } = 0;
    public string ContentType { get; private set; } = "application/octet-stream";
    public int? MaxDownloads { get; private set; }

    // Navigation
    public Shop? Shop { get; private set; }
    public ProductVariant? Variant { get; private set; }

    protected DigitalFile() { }

    public DigitalFile(
        Guid shopId,
        Guid variantId,
        string fileName,
        string storagePath,
        long fileSizeBytes,
        string contentType = "application/octet-stream",
        int? maxDownloads = null)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (variantId == Guid.Empty) throw new ArgumentException("VariantId không hợp lệ.", nameof(variantId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Tên tệp không được để trống.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Đường dẫn lưu trữ không được để trống.", nameof(storagePath));

        ShopId = shopId;
        VariantId = variantId;
        FileName = fileName.Trim();
        StoragePath = storagePath.Trim();
        FileSizeBytes = fileSizeBytes;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        MaxDownloads = maxDownloads;
    }

    public void UpdateFile(string fileName, string storagePath, long fileSizeBytes, string contentType, int? maxDownloads)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Tên tệp không được để trống.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Đường dẫn lưu trữ không được để trống.", nameof(storagePath));

        FileName = fileName.Trim();
        StoragePath = storagePath.Trim();
        FileSizeBytes = fileSizeBytes;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        MaxDownloads = maxDownloads;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
