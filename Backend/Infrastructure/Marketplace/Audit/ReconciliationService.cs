using Application.DTOs.Marketplace.Audit;
using Application.Interfaces.Marketplace.Audit;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Ledger;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Audit;

public class ReconciliationService : IReconciliationService
{
    private readonly SocialDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReconciliationService> _logger;

    private static readonly PayoutStatus[] ActivePayoutStatuses =
    {
        PayoutStatus.Requested,
        PayoutStatus.Approved,
        PayoutStatus.Processing,
        PayoutStatus.Unknown
    };

    public ReconciliationService(
        SocialDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<ReconciliationService> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReconciliationSummaryDto> RunReconciliationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu tiến trình kiểm tra đối soát tự động toàn hệ thống...");

        // 1. Kiểm tra tính toàn vẹn sổ cái (Debit == Credit)
        var totalDebit = await _dbContext.LedgerEntries.SumAsync(e => (long)e.Debit, cancellationToken);
        var totalCredit = await _dbContext.LedgerEntries.SumAsync(e => (long)e.Credit, cancellationToken);
        var totalTx = await _dbContext.LedgerTransactions.CountAsync(cancellationToken);
        var totalEntries = await _dbContext.LedgerEntries.CountAsync(cancellationToken);

        var integrityReport = new LedgerIntegrityReportDto
        {
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            TotalTransactions = totalTx,
            TotalEntries = totalEntries
        };

        // 2. Đối soát số dư các Shop giữa ShopBalanceCache và Sổ cái kép
        var shopCaches = await _dbContext.ShopBalanceCaches
            .Include(c => c.Shop)
            .ToListAsync(cancellationToken);

        var mismatchedShops = new List<ShopBalanceAuditDto>();

        foreach (var cache in shopCaches)
        {
            var audit = await CalculateShopRealBalancesAsync(cache, cancellationToken);
            if (!audit.IsMatched)
            {
                mismatchedShops.Add(audit);
                _logger.LogWarning("Phát hiện lệch số dư tại Shop {ShopId} ({ShopName}): Cache (Avail={CA}, Pend={CP}, PayoutPend={CPP}) vs Ledger (Avail={RA}, Pend={RP}, PayoutPend={RPP})",
                    cache.ShopId, audit.ShopName,
                    audit.CacheAvailable, audit.CachePending, audit.CachePayoutPending,
                    audit.RealAvailable, audit.RealPending, audit.RealPayoutPending);
            }
        }

        // 3. Đối soát Payouts đang mở với tổng số dư PayoutPending trong Ledger
        var payoutPendingAccountIds = await _dbContext.LedgerAccounts
            .Where(a => a.Type == LedgerAccountType.PayoutPending)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        long ledgerPayoutPendingTotal = 0;
        if (payoutPendingAccountIds.Count > 0)
        {
            var pEntries = await _dbContext.LedgerEntries
                .Where(e => payoutPendingAccountIds.Contains(e.AccountId))
                .GroupBy(e => 1)
                .Select(g => new
                {
                    Credit = g.Sum(e => (long)e.Credit),
                    Debit = g.Sum(e => (long)e.Debit)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (pEntries != null)
            {
                ledgerPayoutPendingTotal = pEntries.Credit - pEntries.Debit;
            }
        }

        var activePayouts = await _dbContext.Payouts
            .Where(p => ActivePayoutStatuses.Contains(p.Status))
            .ToListAsync(cancellationToken);

        var activePayoutsTotal = activePayouts.Sum(p => p.Amount);

        var payoutAudit = new PayoutAuditDto
        {
            LedgerPayoutPendingTotal = ledgerPayoutPendingTotal,
            ActivePayoutsTotal = activePayoutsTotal,
            ActivePayoutCount = activePayouts.Count
        };

        var isHealthy = integrityReport.IsBalanced &&
                        mismatchedShops.Count == 0 &&
                        payoutAudit.IsMatched;

        _logger.LogInformation("Kết thúc đối soát: Toàn vẹn sổ cái = {IsBalanced}, Số shop bị lệch = {MismatchedCount}, Khớp PayoutPending = {PayoutMatched}. Trạng thái tổng quan = {Status}",
            integrityReport.IsBalanced, mismatchedShops.Count, payoutAudit.IsMatched, isHealthy ? "HEALTHY" : "WARNING");

        return new ReconciliationSummaryDto
        {
            AuditedAtUtc = DateTime.UtcNow,
            IsSystemHealthy = isHealthy,
            LedgerIntegrity = integrityReport,
            PayoutAudit = payoutAudit,
            MismatchedShops = mismatchedShops,
            TotalShopsAudited = shopCaches.Count
        };
    }

    public async Task<ShopBalanceAuditDto> ReconcileAndFixShopBalanceAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var cache = await _dbContext.ShopBalanceCaches
            .Include(c => c.Shop)
            .FirstOrDefaultAsync(c => c.ShopId == shopId, cancellationToken);

        if (cache == null)
            throw new KeyNotFoundException($"Không tìm thấy bản ghi số dư của Shop {shopId}.");

        var audit = await CalculateShopRealBalancesAsync(cache, cancellationToken);

        if (!audit.IsMatched)
        {
            _logger.LogInformation("Tiến hành tự động đồng bộ lại cache số dư cho Shop {ShopId} ({ShopName}) từ Sổ cái kép...",
                shopId, audit.ShopName);

            cache.SyncFromLedger(audit.RealAvailable, audit.RealPending, audit.RealPayoutPending);
            _unitOfWork.Ledgers.UpdateBalanceCache(cache);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã đồng bộ thành công Shop {ShopId}: Available={Avail}, Pending={Pend}, PayoutPending={PayoutPend}.",
                shopId, audit.RealAvailable, audit.RealPending, audit.RealPayoutPending);

            // Cập nhật lại audit dto sau khi sửa
            audit.CacheAvailable = audit.RealAvailable;
            audit.CachePending = audit.RealPending;
            audit.CachePayoutPending = audit.RealPayoutPending;
        }

        return audit;
    }

    private async Task<ShopBalanceAuditDto> CalculateShopRealBalancesAsync(
        ShopBalanceCache cache,
        CancellationToken cancellationToken)
    {
        var shopAccounts = await _dbContext.LedgerAccounts
            .Where(a => a.ShopId == cache.ShopId)
            .ToListAsync(cancellationToken);

        var availableAccId = shopAccounts.FirstOrDefault(a => a.Type == LedgerAccountType.ShopAvailable)?.Id;
        var pendingAccId = shopAccounts.FirstOrDefault(a => a.Type == LedgerAccountType.ShopPending)?.Id;
        var payoutPendingAccId = shopAccounts.FirstOrDefault(a => a.Type == LedgerAccountType.PayoutPending)?.Id;

        long realAvailable = 0;
        long realPending = 0;
        long realPayoutPending = 0;

        if (availableAccId.HasValue)
        {
            realAvailable = await CalculateAccountBalanceAsync(availableAccId.Value, cancellationToken);
        }

        if (pendingAccId.HasValue)
        {
            realPending = await CalculateAccountBalanceAsync(pendingAccId.Value, cancellationToken);
        }

        if (payoutPendingAccId.HasValue)
        {
            realPayoutPending = await CalculateAccountBalanceAsync(payoutPendingAccId.Value, cancellationToken);
        }

        return new ShopBalanceAuditDto
        {
            ShopId = cache.ShopId,
            ShopName = cache.Shop?.Name ?? string.Empty,
            CacheAvailable = cache.AvailableBalance,
            RealAvailable = realAvailable,
            CachePending = cache.PendingBalance,
            RealPending = realPending,
            CachePayoutPending = cache.PayoutPendingBalance,
            RealPayoutPending = realPayoutPending
        };
    }

    private async Task<long> CalculateAccountBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var sum = await _dbContext.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .GroupBy(e => 1)
            .Select(g => new
            {
                Credit = g.Sum(e => (long)e.Credit),
                Debit = g.Sum(e => (long)e.Debit)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return sum == null ? 0 : (sum.Credit - sum.Debit);
    }
}
