using Domain.Common;

namespace Domain.Entities.Marketplace.Ledger;

/// <summary>
/// Dòng bút toán sổ cái (Append-Only)
/// Ghi Nợ (Debit) hoặc Ghi Có (Credit) vào một LedgerAccount cụ thể.
/// </summary>
public class LedgerEntry : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public long Debit { get; private set; }
    public long Credit { get; private set; }

    // Navigation
    public LedgerTransaction? Transaction { get; private set; }
    public LedgerAccount? Account { get; private set; }

    protected LedgerEntry() { }

    public LedgerEntry(
        Guid transactionId,
        Guid accountId,
        long debit,
        long credit)
    {
        if (transactionId == Guid.Empty) throw new ArgumentException("TransactionId không hợp lệ.", nameof(transactionId));
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId không hợp lệ.", nameof(accountId));
        if (debit < 0) throw new ArgumentOutOfRangeException(nameof(debit), "Số tiền Ghi Nợ không được âm.");
        if (credit < 0) throw new ArgumentOutOfRangeException(nameof(credit), "Số tiền Ghi Có không được âm.");
        if (debit == 0 && credit == 0) throw new ArgumentException("Bút toán phải có số tiền Ghi Nợ hoặc Ghi Có lớn hơn 0.");
        if (debit > 0 && credit > 0) throw new ArgumentException("Một bút toán không được đồng thời vừa Ghi Nợ vừa Ghi Có.");

        TransactionId = transactionId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
    }
}
