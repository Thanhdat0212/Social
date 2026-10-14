using Domain.Enums;

namespace Application.DTOs.Marketplace.Categories;

public class CategoryTreeNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string ModerationPolicy { get; set; } = string.Empty;
    public int? CommissionRateBp { get; set; }
    public List<CategoryTreeNodeDto> Children { get; set; } = new();
}

public class CategoryDetailDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string ModerationPolicy { get; set; } = string.Empty;
    public int? CommissionRateBp { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<CategoryAttributeDto> Attributes { get; set; } = new();
}

public class CreateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public ProductType ProductType { get; set; } = ProductType.Digital;
    public CategoryModerationPolicy ModerationPolicy { get; set; } = CategoryModerationPolicy.DirectListing;
    public int? CommissionRateBp { get; set; }
}

public class UpdateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public ProductType ProductType { get; set; }
    public CategoryModerationPolicy ModerationPolicy { get; set; }
    public int? CommissionRateBp { get; set; }
}

public class AttributeDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<AttributeOptionDto> Options { get; set; } = new();
}

public class AttributeOptionDto
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAttributeRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AttributeDataType DataType { get; set; }
    public string? Unit { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<string>? InitialOptions { get; set; }
}

public class UpdateAttributeRequestDto
{
    public string Name { get; set; } = string.Empty;
    public AttributeDataType DataType { get; set; }
    public string? Unit { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

public class CreateAttributeOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int DisplayOrder { get; set; }
}

public class CategoryAttributeDto
{
    public Guid AttributeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public bool IsFilterable { get; set; }
    public bool IsVariantAxis { get; set; }
    public Guid? InheritedFromCategoryId { get; set; }
    public List<AttributeOptionDto> Options { get; set; } = new();
}

public class AssignCategoryAttributeRequestDto
{
    public Guid AttributeId { get; set; }
    public AttributeRequirement Requirement { get; set; } = AttributeRequirement.Optional;
    public bool IsFilterable { get; set; }
    public bool IsVariantAxis { get; set; }
}

public class CreateCategoryRequestFromSellerDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? SuggestedParentId { get; set; }
    public string? ReasonNote { get; set; }
}

public class CategoryRequestDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? SuggestedParentId { get; set; }
    public string? SuggestedParentName { get; set; }
    public string? ReasonNote { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public Guid? ResolvedCategoryId { get; set; }
    public string? ResolvedCategoryName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public class ReviewCategoryRequestDto
{
    public bool IsApproved { get; set; }
    public Guid? ResolvedCategoryId { get; set; }
    public string? AdminNote { get; set; }
}
