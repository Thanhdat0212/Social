using Domain.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Reports;

/// <summary>
/// Báo cáo vi phạm của sản phẩm hoặc gian hàng (Chống lạm dụng & Bảo vệ người mua)
/// </summary>
public class ProductReport : BaseEntity
{
    public Guid ReporterUserId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ShopId { get; private set; }
    public ProductReportReason Reason { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? EvidenceUrl { get; private set; }
    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public string? AdminResolutionNote { get; private set; }
    public Guid? ResolvedByAdminId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    // Navigation
    public User? ReporterUser { get; private set; }
    public Product? Product { get; private set; }
    public Shop? Shop { get; private set; }
    public User? ResolvedByAdmin { get; private set; }

    protected ProductReport() { }

    public ProductReport(
        Guid reporterUserId,
        Guid productId,
        Guid shopId,
        ProductReportReason reason,
        string description,
        string? evidenceUrl = null)
    {
        if (reporterUserId == Guid.Empty) throw new ArgumentException("ReporterUserId không hợp lệ.", nameof(reporterUserId));
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Mô tả vi phạm không được để trống.", nameof(description));

        ReporterUserId = reporterUserId;
        ProductId = productId;
        ShopId = shopId;
        Reason = reason;
        Description = description.Trim();
        EvidenceUrl = evidenceUrl?.Trim();
        Status = ReportStatus.Pending;
    }

    public void Resolve(Guid adminId, ReportStatus resolutionStatus, string note)
    {
        if (resolutionStatus != ReportStatus.ActionTaken && resolutionStatus != ReportStatus.Dismissed)
            throw new ArgumentException("Trạng thái xử lý chỉ có thể là ActionTaken hoặc Dismissed.");

        Status = resolutionStatus;
        AdminResolutionNote = note;
        ResolvedByAdminId = adminId;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
