namespace Application.DTOs.Marketplace.Shops;

public class CreateShopRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
}

public class UpdateShopRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
}

public class ResubmitShopRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
}

public class RejectShopRequestDto
{
    public string? ReasonCode { get; set; }
    public string ReasonNote { get; set; } = string.Empty;
    public bool CanResubmit { get; set; } = true;
}

public class CreateSanctionRequestDto
{
    public Domain.Enums.ShopSanctionType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? EndAtUtc { get; set; }
}

public class ShopSummaryDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string OperatingStatus { get; set; } = string.Empty;
    public string EffectiveStatus { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class ShopDetailDto : ShopSummaryDto
{
    public string? Description { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string? EffectiveStatusReason { get; set; }
    public bool CanResubmit { get; set; } = true;
    public DateTime? UpdatedAtUtc { get; set; }
    public ShopReviewDto? LatestReview { get; set; }
    public ShopSanctionDto? ActiveSanction { get; set; }
    public List<ShopReviewDto> Reviews { get; set; } = new();
    public List<ShopSanctionDto> Sanctions { get; set; } = new();
}

public class ShopReviewDto
{
    public Guid Id { get; set; }
    public int SubmissionNo { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public string? ReasonNote { get; set; }
    public bool CanResubmit { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? SnapshotJson { get; set; }
}

public class ShopSanctionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LiftedAtUtc { get; set; }
}
