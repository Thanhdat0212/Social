using Application.Interfaces.Repositories.Marketplace.Complaints;
using Domain.Entities.Marketplace.Complaints;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Marketplace.Complaints;

public class ComplaintRepository : IComplaintRepository
{
    private readonly SocialDbContext _dbContext;

    public ComplaintRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Complaints
            .Include(c => c.Evidences)
            .Include(c => c.ShopOrder)
            .Include(c => c.OrderItem)
            .Include(c => c.Shop)
            .Include(c => c.BuyerUser)
            .Include(c => c.ResolvedByAdmin)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Complaint complaint, CancellationToken cancellationToken = default)
    {
        await _dbContext.Complaints.AddAsync(complaint, cancellationToken);
    }

    public void Update(Complaint complaint)
    {
        _dbContext.Complaints.Update(complaint);
    }

    public async Task<(List<Complaint> Items, int TotalCount)> GetBuyerComplaintsAsync(
        Guid buyerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Complaints
            .Include(c => c.Evidences)
            .Include(c => c.ShopOrder)
            .Include(c => c.OrderItem)
            .Include(c => c.Shop)
            .Where(c => c.BuyerUserId == buyerUserId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<Complaint> Items, int TotalCount)> GetShopComplaintsAsync(
        Guid shopId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Complaints
            .Include(c => c.Evidences)
            .Include(c => c.ShopOrder)
            .Include(c => c.OrderItem)
            .Where(c => c.ShopId == shopId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<Complaint> Items, int TotalCount)> GetAdminComplaintsAsync(
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Complaints
            .Include(c => c.Evidences)
            .Include(c => c.ShopOrder)
            .Include(c => c.OrderItem)
            .Include(c => c.Shop)
            .Include(c => c.BuyerUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }
        else
        {
            // Mặc định hiện các khiếu nại đang cần Admin giải quyết
            query = query.Where(c => c.Status == ComplaintStatus.EscalatedToAdmin ||
                                     c.Status == ComplaintStatus.ShopRejected ||
                                     c.Status == ComplaintStatus.Submitted);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.ShopDeadlineAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> HasActiveComplaintForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Complaints
            .AnyAsync(c => c.OrderItemId == orderItemId &&
                           c.Status != ComplaintStatus.Rejected &&
                           c.Status != ComplaintStatus.Cancelled &&
                           c.Status != ComplaintStatus.ResolvedRefund &&
                           c.Status != ComplaintStatus.ResolvedReplacement,
                           cancellationToken);
    }

    public async Task<bool> HasActiveComplaintsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Complaints
            .AnyAsync(c => c.ShopOrderId == orderId &&
                           c.Status != ComplaintStatus.Rejected &&
                           c.Status != ComplaintStatus.Cancelled &&
                           c.Status != ComplaintStatus.ResolvedRefund &&
                           c.Status != ComplaintStatus.ResolvedReplacement,
                           cancellationToken);
    }

    public async Task<IReadOnlyList<Complaint>> GetOverdueSubmittedComplaintsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Complaints
            .Where(c => c.Status == ComplaintStatus.Submitted && c.ShopDeadlineAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }
}

public class RefundRepository : IRefundRepository
{
    private readonly SocialDbContext _dbContext;

    public RefundRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .Include(r => r.Payment)
            .Include(r => r.ShopOrder)
            .Include(r => r.OrderItem)
            .Include(r => r.Complaint)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await _dbContext.Refunds.AddAsync(refund, cancellationToken);
    }

    public void Update(Refund refund)
    {
        _dbContext.Refunds.Update(refund);
    }

    public async Task<IReadOnlyList<Refund>> GetRefundsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .Where(r => r.ShopOrderId == orderId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Refund>> GetPendingRefundsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .Where(r => r.Status == RefundStatus.Pending)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
