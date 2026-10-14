using Domain.Common;

namespace Domain.Entities.Marketplace.Products;

/// <summary>
/// Giá trị của trục tùy chọn (Ví dụ: "1 Năm", "Trọn đời", "Windows", "MacOS")
/// </summary>
public class ProductOptionValue : BaseEntity
{
    public Guid OptionId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public int Position { get; private set; } = 0;

    // Navigation
    public ProductOption? Option { get; private set; }
    public ICollection<VariantOptionValue> VariantValues { get; private set; } = new List<VariantOptionValue>();

    protected ProductOptionValue() { }

    public ProductOptionValue(Guid optionId, string value, int position = 0)
    {
        if (optionId == Guid.Empty) throw new ArgumentException("OptionId không hợp lệ.", nameof(optionId));
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Giá trị tùy chọn không được để trống.", nameof(value));

        OptionId = optionId;
        Value = value.Trim();
        Position = position;
    }

    public void Update(string value, int position)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Giá trị tùy chọn không được để trống.", nameof(value));
        Value = value.Trim();
        Position = position;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
