using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Marketplace.Products;

#region Requests

public class CreateProductRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [MaxLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    [MaxLength(10000, ErrorMessage = "Mô tả sản phẩm tối đa 10,000 ký tự")]
    public string? Description { get; set; }

    // Thông tin biến thể mặc định khi sản phẩm chưa có nhiều biến thể
    public DefaultVariantInput? DefaultVariant { get; set; }
}

public class DefaultVariantInput
{
    [Required(ErrorMessage = "Giá bán biến thể không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
    public long Price { get; set; }

    public long? CompareAtPrice { get; set; }

    [MaxLength(100, ErrorMessage = "SKU tối đa 100 ký tự")]
    public string? Sku { get; set; }

    public DeliveryType DeliveryType { get; set; } = DeliveryType.CodePool;
    public bool TrackInventory { get; set; } = true;
}

public class UpdateProductDetailsRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [MaxLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    [MaxLength(10000, ErrorMessage = "Mô tả sản phẩm tối đa 10,000 ký tự")]
    public string? Description { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
    public long? Price { get; set; }

    public long? CompareAtPrice { get; set; }
}

public class UpdateVariantPriceRequest
{
    [Required(ErrorMessage = "Giá bán không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
    public long Price { get; set; }

    public long? CompareAtPrice { get; set; }
}

public class CreateProductOptionInput
{
    [Required(ErrorMessage = "Tên tùy chọn không được để trống")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Position { get; set; } = 0;

    [Required]
    [MinLength(1, ErrorMessage = "Tùy chọn phải có ít nhất 1 giá trị")]
    public List<string> Values { get; set; } = new();
}

public class ProductVariantInput
{
    public Guid? Id { get; set; }

    [MaxLength(100)]
    public string? Sku { get; set; }

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Giá biến thể phải lớn hơn 0")]
    public long Price { get; set; }

    public long? CompareAtPrice { get; set; }

    public DeliveryType DeliveryType { get; set; } = DeliveryType.CodePool;
    public bool TrackInventory { get; set; } = true;

    // Danh sách tên giá trị tùy chọn cấu thành nên biến thể (VD: ["1 Năm", "Pro"])
    public List<string> OptionValues { get; set; } = new();
}

public class UpdateProductVariantsRequest
{
    public List<CreateProductOptionInput> Options { get; set; } = new();

    [Required]
    [MinLength(1, ErrorMessage = "Sản phẩm phải có ít nhất 1 biến thể")]
    public List<ProductVariantInput> Variants { get; set; } = new();
}

public class ProductAttributeValueInput
{
    [Required]
    public Guid AttributeId { get; set; }

    public Guid? OptionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BoolValue { get; set; }
}

public class SaveProductAttributeValuesRequest
{
    public List<ProductAttributeValueInput> Attributes { get; set; } = new();
}

public class UploadProductImageRequest
{
    [Required]
    [MaxLength(1000)]
    public string Url { get; set; } = string.Empty;

    public int Position { get; set; } = 0;
    public bool IsCover { get; set; } = false;
}

public class ReorderProductImagesRequest
{
    public List<ProductImageOrderItem> Images { get; set; } = new();
}

public class ProductImageOrderItem
{
    public Guid ImageId { get; set; }
    public int Position { get; set; }
    public bool IsCover { get; set; }
}

public class TakedownProductRequest
{
    [Required(ErrorMessage = "Lý do gỡ bỏ không được để trống")]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class CatalogFilterQuery
{
    public Guid? CategoryId { get; set; }
    public string? Search { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public string? Sort { get; set; } // "newest", "price_asc", "price_desc", "name"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

#endregion

#region Responses

public class ProductDetailDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public bool HasActiveTakedown { get; set; }
    public string? ActiveTakedownReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ProductOptionDto> Options { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductAttributeValueDto> AttributeValues { get; set; } = new();
}

public class ProductOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<ProductOptionValueDto> Values { get; set; } = new();
}

public class ProductOptionValueDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string CombinationKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public long? CompareAtPrice { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public bool TrackInventory { get; set; }
    public int AvailableStock { get; set; }
    public List<string> OptionValues { get; set; } = new();
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsCover { get; set; }
}

public class ProductAttributeValueDto
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public Guid? OptionId { get; set; }
    public string? OptionValue { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BoolValue { get; set; }
}

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public int VariantCount { get; set; }
    public List<ProductVariantDto> Variants { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public bool HasActiveTakedown { get; set; }
    public string? ShopName { get; set; }
}


public class CatalogProductCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CoverImageUrl { get; set; }
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public double RatingAverage { get; set; } = 0.0;
    public int ReviewCount { get; set; } = 0;
    public DateTime CreatedAtUtc { get; set; }
}

public class CatalogProductDetailDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public double RatingAverage { get; set; } = 0.0;
    public int ReviewCount { get; set; } = 0;
    public DateTime CreatedAtUtc { get; set; }

    public List<ProductOptionDto> Options { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductAttributeValueDto> AttributeValues { get; set; } = new();
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
}

public class ProductMediaUploadResponseDto
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}

#endregion
