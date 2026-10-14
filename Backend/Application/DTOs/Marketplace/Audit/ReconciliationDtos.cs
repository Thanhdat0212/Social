namespace Application.DTOs.Marketplace.Audit;

public class LedgerIntegrityReportDto
{
    public long TotalDebit { get; set; }
    public long TotalCredit { get; set; }
    public bool IsBalanced => TotalDebit == TotalCredit;
    public long Discrepancy => Math.Abs(TotalDebit - TotalCredit);
    public int TotalTransactions { get; set; }
    public int TotalEntries { get; set; }
}

public class ShopBalanceAuditDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public long CacheAvailable { get; set; }
    public long RealAvailable { get; set; }
    public long CachePending { get; set; }
    public long RealPending { get; set; }
    public long CachePayoutPending { get; set; }
    public long RealPayoutPending { get; set; }
    public bool IsMatched =>
        CacheAvailable == RealAvailable &&
        CachePending == RealPending &&
        CachePayoutPending == RealPayoutPending;
}

public class PayoutAuditDto
{
    public long LedgerPayoutPendingTotal { get; set; }
    public long ActivePayoutsTotal { get; set; }
    public bool IsMatched => LedgerPayoutPendingTotal == ActivePayoutsTotal;
    public int ActivePayoutCount { get; set; }
}

public class ReconciliationSummaryDto
{
    public DateTime AuditedAtUtc { get; set; }
    public bool IsSystemHealthy { get; set; }
    public LedgerIntegrityReportDto LedgerIntegrity { get; set; } = new();
    public PayoutAuditDto PayoutAudit { get; set; } = new();
    public List<ShopBalanceAuditDto> MismatchedShops { get; set; } = new();
    public int TotalShopsAudited { get; set; }
}
