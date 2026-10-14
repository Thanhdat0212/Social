namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Bảng nối Nhiều - Nhiều giữa Variant và OptionValue
/// </summary>
public class VariantOptionValue
{
    public Guid VariantId { get; private set; }
    public Guid OptionValueId { get; private set; }

    // Navigation
    public ProductVariant? Variant { get; private set; }
    public ProductOptionValue? OptionValue { get; private set; }

    protected VariantOptionValue() { }

    public VariantOptionValue(Guid variantId, Guid optionValueId)
    {
        if (variantId == Guid.Empty) throw new ArgumentException("VariantId không hợp lệ.", nameof(variantId));
        if (optionValueId == Guid.Empty) throw new ArgumentException("OptionValueId không hợp lệ.", nameof(optionValueId));

        VariantId = variantId;
        OptionValueId = optionValueId;
    }
}
