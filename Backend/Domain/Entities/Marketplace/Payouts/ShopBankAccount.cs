using Domain.Common;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Payouts;

/// <summary>
/// Tài khoản ngân hàng nhận tiền rút của Gian hàng (ShopBankAccount)
/// </summary>
public class ShopBankAccount : BaseEntity
{
    public Guid ShopId { get; private set; }
    public string BankCode { get; private set; } = string.Empty; // Mã ngân hàng: VCB, TCB, MB, ACB, VPB...
    public string BankName { get; private set; } = string.Empty;
    public string AccountNumberEncrypted { get; private set; } = string.Empty; // Mã hóa AES-GCM
    public string AccountNumberMasked { get; private set; } = string.Empty;    // Ví dụ: ****1234
    public string AccountHolderName { get; private set; } = string.Empty;      // Tên in hoa không dấu: NGUYEN VAN A
    public BankAccountStatus Status { get; private set; } = BankAccountStatus.Verified;
    public DateTime? LockedUntilUtc { get; private set; } // Khóa rút 24-72h sau khi cập nhật tài khoản để chống hack

    // Navigation
    public Shop? Shop { get; private set; }

    protected ShopBankAccount() { }

    public ShopBankAccount(
        Guid shopId,
        string bankCode,
        string bankName,
        string accountNumberEncrypted,
        string accountNumberMasked,
        string accountHolderName,
        int lockHours = 24)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(bankCode)) throw new ArgumentException("Mã ngân hàng không được để trống.", nameof(bankCode));
        if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentException("Tên ngân hàng không được để trống.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(accountNumberEncrypted)) throw new ArgumentException("Số tài khoản mã hóa không được để trống.", nameof(accountNumberEncrypted));
        if (string.IsNullOrWhiteSpace(accountHolderName)) throw new ArgumentException("Tên chủ tài khoản không được để trống.", nameof(accountHolderName));

        ShopId = shopId;
        BankCode = bankCode.Trim().ToUpperInvariant();
        BankName = bankName.Trim();
        AccountNumberEncrypted = accountNumberEncrypted.Trim();
        AccountNumberMasked = accountNumberMasked.Trim();
        AccountHolderName = accountHolderName.Trim().ToUpperInvariant();
        Status = BankAccountStatus.Verified;
        LockedUntilUtc = lockHours > 0 ? DateTime.UtcNow.AddHours(lockHours) : null;
    }

    public void UpdateAccount(
        string bankCode,
        string bankName,
        string accountNumberEncrypted,
        string accountNumberMasked,
        string accountHolderName,
        int lockHours = 24)
    {
        BankCode = bankCode.Trim().ToUpperInvariant();
        BankName = bankName.Trim();
        AccountNumberEncrypted = accountNumberEncrypted.Trim();
        AccountNumberMasked = accountNumberMasked.Trim();
        AccountHolderName = accountHolderName.Trim().ToUpperInvariant();
        LockedUntilUtc = lockHours > 0 ? DateTime.UtcNow.AddHours(lockHours) : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        Status = BankAccountStatus.Disabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
