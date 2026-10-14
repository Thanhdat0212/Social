using Domain.Enums;

namespace Application.DTOs.Marketplace.Reports;

public class CreateProductReportRequest
{
    public ProductReportReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
}

public class ProductReportDto
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public ProductReportReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public ReportStatus Status { get; set; }
    public string? AdminResolutionNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class ResolveProductReportRequest
{
    public ReportStatus ResolutionStatus { get; set; } // ActionTaken hoặc Dismissed
    public string AdminNote { get; set; } = string.Empty;
    public bool TakedownProduct { get; set; }
    public bool ApplySanctionToShop { get; set; }
    public string? SanctionReasonCode { get; set; }
}

public class ShopPerformanceDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalComplaints { get; set; }
    public int ValidComplaints { get; set; } // Khiếu nại được phán quyết hoàn tiền / đổi hàng
    public double DefectRate { get; set; }    // % Tỷ lệ đơn lỗi = (ValidComplaints / TotalOrders) * 100
    public bool IsHighRisk { get; set; }     // Cờ rủi ro cao nếu DefectRate > 5% và TotalOrders >= 10
    public string PerformanceTier { get; set; } = "Good"; // "Excellent" (<1%), "Good" (1-3%), "Fair" (3-5%), "HighRisk" (>5%)
}
