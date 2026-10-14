using Domain.Enums;

namespace Application.DTOs.Marketplace.Complaints;

public class CreateComplaintRequest
{
    public ComplaintReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public int FaultyQuantity { get; set; } = 1;
    public List<string> EvidenceUrls { get; set; } = new();
}

public class ShopRespondComplaintRequest
{
    public bool Accept { get; set; }
    public string ResolutionType { get; set; } = "Refund"; // "Refund" | "Replacement"
    public string Note { get; set; } = string.Empty;
}

public class AdminResolveComplaintRequest
{
    public string Resolution { get; set; } = "Refund"; // "Refund" | "Replacement" | "Reject"
    public RefundBearer Bearer { get; set; } = RefundBearer.Shop;
    public string Note { get; set; } = string.Empty;
}

public class ComplaintEvidenceDto
{
    public Guid Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = "Image";
    public string? Note { get; set; }
}

public class ComplaintListItemDto
{
    public Guid Id { get; set; }
    public Guid ShopOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public int FaultyQuantity { get; set; }
    public ComplaintReason Reason { get; set; }
    public ComplaintStatus Status { get; set; }
    public DateTime ShopDeadlineAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ComplaintDetailDto
{
    public Guid Id { get; set; }
    public Guid ShopOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int QuantityBought { get; set; }
    public int FaultyQuantity { get; set; }
    public ComplaintReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; }
    public DateTime ShopDeadlineAt { get; set; }

    public string? ShopResponseNote { get; set; }
    public DateTime? ShopRespondedAt { get; set; }

    public string? AdminResolutionNote { get; set; }
    public string? ResolutionType { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public List<ComplaintEvidenceDto> Evidences { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RefundDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ShopOrderId { get; set; }
    public long Amount { get; set; }
    public RefundBearer Bearer { get; set; }
    public long ShopDeductionAmount { get; set; }
    public long PlatformDeductionAmount { get; set; }
    public RefundStatus Status { get; set; }
    public string? ReasonCode { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
