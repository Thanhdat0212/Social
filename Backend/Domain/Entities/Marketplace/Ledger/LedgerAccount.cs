using Domain.Common;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Ledger;

/// <summary>
/// Tài khoản kế toán trong hệ thống Sổ cái kép (Double-Entry Ledger)
/// Có thể là tài khoản hệ thống (PlatformRevenue, GatewayClearing,...) hoặc tài khoản con của từng Shop (ShopPending, ShopAvailable,...)
/// </summary>
public class LedgerAccount : BaseEntity
{
    public LedgerAccountType Type { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ShopId { get; private set; }
    public string Currency { get; private set; } = "VND";
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Shop? Shop { get; private set; }
    public ICollection<LedgerEntry> Entries { get; private set; } = new List<LedgerEntry>();

    protected LedgerAccount() { }

    public LedgerAccount(
        LedgerAccountType type,
        string code,
        string name,
        Guid? shopId = null,
        string currency = "VND")
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Mã tài khoản không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên tài khoản không được để trống.", nameof(name));

        Type = type;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ShopId = shopId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        IsActive = true;
    }

    public static string GenerateAccountCode(LedgerAccountType type, Guid? shopId = null)
    {
        return shopId.HasValue
            ? $"{type.ToString().ToUpperInvariant()}_{shopId.Value:N}"
            : type.ToString().ToUpperInvariant();
    }
}
