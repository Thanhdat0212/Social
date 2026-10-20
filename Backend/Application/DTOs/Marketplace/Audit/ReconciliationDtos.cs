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

public class DiscrepantShopDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public long CachedAvailable { get; set; }
    public long ComputedAvailable { get; set; }
    public long Difference { get; set; }
}

public class ReconciliationSummaryDto
{
    public DateTime AuditedAtUtc { get; set; }
    public DateTime RunAtUtc => AuditedAtUtc;
    public bool IsSystemHealthy { get; set; }
    public LedgerIntegrityReportDto LedgerIntegrity { get; set; } = new();
    public PayoutAuditDto PayoutAudit { get; set; } = new();
    public List<ShopBalanceAuditDto> MismatchedShops { get; set; } = new();
    public int TotalShopsAudited { get; set; }

    // Backward-compatible properties matching Frontend expectations
    public bool IsBalanced => LedgerIntegrity.IsBalanced;
    public long TotalDebit => LedgerIntegrity.TotalDebit;
    public long TotalCredit => LedgerIntegrity.TotalCredit;
    public long Discrepancy => LedgerIntegrity.Discrepancy;
    public long ActivePayoutsTotal => PayoutAudit.ActivePayoutsTotal;
    public long PayoutPendingBalance => PayoutAudit.LedgerPayoutPendingTotal;
    public bool PayoutsMatch => PayoutAudit.IsMatched;
    public int DiscrepantShopCount => MismatchedShops.Count;
    public List<DiscrepantShopDto> DiscrepantShops => MismatchedShops.Select(s => new DiscrepantShopDto
    {
        ShopId = s.ShopId,
        ShopName = s.ShopName,
        CachedAvailable = s.CacheAvailable,
        ComputedAvailable = s.RealAvailable,
        Difference = Math.Abs(s.CacheAvailable - s.RealAvailable)
    }).ToList();
}
