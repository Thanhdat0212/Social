using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Categories;

/// <summary>
/// Định nghĩa thuộc tính sản phẩm (Bộ từ điển thuộc tính dùng chung)
/// </summary>
public class AttributeDefinition : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AttributeDataType DataType { get; private set; } = AttributeDataType.Text;
    public string? Unit { get; private set; }
    public decimal? Min { get; private set; }
    public decimal? Max { get; private set; }

    // Navigation properties
    public ICollection<AttributeOption> Options { get; private set; } = new List<AttributeOption>();
    public ICollection<CategoryAttribute> CategoryAttributes { get; private set; } = new List<CategoryAttribute>();

    protected AttributeDefinition() { }

    public AttributeDefinition(
        string code,
        string name,
        AttributeDataType dataType,
        string? unit = null,
        decimal? min = null,
        decimal? max = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Mã thuộc tính không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên thuộc tính không được để trống.", nameof(name));

        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        DataType = dataType;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Min = min;
        Max = max;
    }

    public void Update(
        string name,
        AttributeDataType dataType,
        string? unit,
        decimal? min,
        decimal? max)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên thuộc tính không được để trống.", nameof(name));

        Name = name.Trim();
        DataType = dataType;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Min = min;
        Max = max;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
