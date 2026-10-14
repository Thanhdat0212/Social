using Application.Common;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Products;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;

namespace Application.Interfaces.Marketplace.Ledger;

public interface ILedgerService
{
    /// <summary>
    /// Ghi sổ kép tổng quát (kiểm tra Debit == Credit, Idempotency, cập nhật ShopBalanceCache nếu liên quan tới shop)
    /// </summary>
    Task PostTransactionAsync(
        LedgerTransactionType type,
        string refType,
        Guid refId,
        string idempotencyKey,
        string memo,
        List<LedgerPostingLine> lines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Giao dịch B: Phân bổ tiền sau khi đơn hàng thành công (Nợ UnallocatedPayments, Có ShopPending theo từng Shop, Có PlatformRevenue)
    /// </summary>
    Task SettleCheckoutPaymentAsync(
        Guid checkoutId,
        Payment payment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Giải phóng tiền khi đơn hàng hoàn tất (hết 48h khiếu nại hoặc người mua bấm Đã nhận tốt): Nợ ShopPending, Có ShopAvailable
    /// </summary>
    Task ReleaseOrderFundsAsync(
        Guid shopOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xem số dư ví của Shop (Pending, Available, PayoutPending)
    /// </summary>
    Task<ShopBalanceDto> GetShopBalanceAsync(
        Guid sellerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xem sao kê tài chính các bút toán sổ cái của Shop
    /// </summary>
    Task<PagedResult<LedgerStatementItemDto>> GetShopStatementAsync(
        Guid sellerUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
