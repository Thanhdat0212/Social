using Domain.Entities.Marketplace.Complaints;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Complaints;

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Complaint complaint, CancellationToken cancellationToken = default);
    void Update(Complaint complaint);

    Task<(List<Complaint> Items, int TotalCount)> GetBuyerComplaintsAsync(
        Guid buyerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<Complaint> Items, int TotalCount)> GetShopComplaintsAsync(
        Guid shopId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<Complaint> Items, int TotalCount)> GetAdminComplaintsAsync(
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveComplaintForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveComplaintsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Complaint>> GetOverdueSubmittedComplaintsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);
    void Update(Refund refund);
    Task<IReadOnlyList<Refund>> GetRefundsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Refund>> GetPendingRefundsAsync(CancellationToken cancellationToken = default);
}
