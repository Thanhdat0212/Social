using Domain.Enums;

namespace Application.DTOs.Marketplace.Ledger;

public class ShopBalanceDto
{
    public Guid ShopId { get; set; }
    public long PendingBalance { get; set; }       // Tiền tạm giữ trong cửa sổ khiếu nại (VND)
    public long AvailableBalance { get; set; }     // Tiền khả dụng có thể rút (VND)
    public long PayoutPendingBalance { get; set; } // Tiền đang chờ xử lý rút (VND)
    public long TotalBalance => PendingBalance + AvailableBalance; // Tổng tài sản của Shop trên sàn
    public long LifetimeGrossRevenue { get; set; } // Doanh thu tích lũy (VND)
    public long LifetimeCommissionPaid { get; set; } // Phí sàn đã trả (VND)
    public DateTime LastUpdatedAt { get; set; }
}

public class LedgerPostingLine
{
    public Guid AccountId { get; set; }
    public long Debit { get; set; }
    public long Credit { get; set; }
}

public class LedgerStatementItemDto
{
    public Guid EntryId { get; set; }
    public Guid TransactionId { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long Debit { get; set; }
    public long Credit { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string RefType { get; set; } = string.Empty;
    public Guid RefId { get; set; }
    public DateTime CreatedAt { get; set; }
}
