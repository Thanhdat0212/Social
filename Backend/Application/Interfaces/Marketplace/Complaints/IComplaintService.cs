using Application.DTOs.Marketplace.Complaints;
using Application.DTOs.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Complaints;

public interface IComplaintService
{
    // Buyer
    Task<ComplaintDetailDto> CreateComplaintAsync(
        Guid buyerUserId,
        Guid orderId,
        Guid orderItemId,
        CreateComplaintRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ComplaintListItemDto>> GetBuyerComplaintsAsync(
        Guid buyerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ComplaintDetailDto?> GetBuyerComplaintByIdAsync(
        Guid buyerUserId,
        Guid complaintId,
        CancellationToken cancellationToken = default);

    Task CancelComplaintAsync(
        Guid buyerUserId,
        Guid complaintId,
        CancellationToken cancellationToken = default);

    // Shop
    Task<PagedResult<ComplaintListItemDto>> GetShopComplaintsAsync(
        Guid sellerUserId,
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task RespondComplaintAsync(
        Guid sellerUserId,
        Guid complaintId,
        ShopRespondComplaintRequest request,
        CancellationToken cancellationToken = default);

    // Admin
    Task<PagedResult<ComplaintListItemDto>> GetAdminComplaintsAsync(
        ComplaintStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task ResolveComplaintAsync(
        Guid adminId,
        Guid complaintId,
        AdminResolveComplaintRequest request,
        CancellationToken cancellationToken = default);
}
