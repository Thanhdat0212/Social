using Application.Common;
using Application.DTOs.Marketplace.Payouts;
using Application.DTOs.Marketplace.Products;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Payouts;

public interface IPayoutService
{
    // Shop Owner
    Task<ShopBankAccountDto> SetBankAccountAsync(
        Guid sellerUserId,
        CreateShopBankAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<ShopBankAccountDto?> GetBankAccountAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> RequestPayoutAsync(
        Guid sellerUserId,
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PayoutDto>> GetShopPayoutsAsync(
        Guid sellerUserId,
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> GetShopPayoutDetailAsync(
        Guid sellerUserId,
        Guid payoutId,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> CancelPayoutAsync(
        Guid sellerUserId,
        Guid payoutId,
        ShopCancelPayoutRequest request,
        CancellationToken cancellationToken = default);

    // Admin
    Task<PagedResult<PayoutDto>> GetAdminPayoutsAsync(
        PayoutStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> GetAdminPayoutDetailAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> ApprovePayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminApprovePayoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> RejectPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminRejectPayoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> MarkPaidPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminMarkPaidPayoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PayoutDto> MarkUnknownPayoutAsync(
        Guid adminId,
        Guid payoutId,
        AdminMarkUnknownPayoutRequest request,
        CancellationToken cancellationToken = default);
}
