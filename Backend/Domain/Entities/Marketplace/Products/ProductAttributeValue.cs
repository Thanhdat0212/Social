using Domain.Common;
using Domain.Entities.Marketplace.Categories;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Giá trị thuộc tính động gắn vào sản phẩm (Attribute Value)
/// </summary>
public class ProductAttributeValue : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid AttributeId { get; private set; }
    public Guid? OptionId { get; private set; }
    public string? TextValue { get; private set; }
    public decimal? NumberValue { get; private set; }
    public bool? BoolValue { get; private set; }

    // Navigation
    public Product? Product { get; private set; }
    public AttributeDefinition? Attribute { get; private set; }
    public AttributeOption? Option { get; private set; }

    protected ProductAttributeValue() { }

    public ProductAttributeValue(
        Guid productId,
        Guid attributeId,
        Guid? optionId = null,
        string? textValue = null,
        decimal? numberValue = null,
        bool? boolValue = null)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId không hợp lệ.", nameof(productId));
        if (attributeId == Guid.Empty) throw new ArgumentException("AttributeId không hợp lệ.", nameof(attributeId));

        ProductId = productId;
        AttributeId = attributeId;
        OptionId = optionId;
        TextValue = textValue?.Trim();
        NumberValue = numberValue;
        BoolValue = boolValue;
    }

    public void Update(Guid? optionId, string? textValue, decimal? numberValue, bool? boolValue)
    {
        OptionId = optionId;
        TextValue = textValue?.Trim();
        NumberValue = numberValue;
        BoolValue = boolValue;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
