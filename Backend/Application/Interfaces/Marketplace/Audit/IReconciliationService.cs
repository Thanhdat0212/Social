using Application.DTOs.Marketplace.Audit;

namespace Application.Interfaces.Marketplace.Audit;

public interface IReconciliationService
{
    /// <summary>
    /// Chạy tiến trình đối soát kiểm tra toàn vẹn sổ cái và tính khớp số dư các gian hàng
    /// </summary>
    Task<ReconciliationSummaryDto> RunReconciliationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tự động tính toán lại từ sổ cái kép và đồng bộ sửa ShopBalanceCache bị lệch
    /// </summary>
    Task<ShopBalanceAuditDto> ReconcileAndFixShopBalanceAsync(Guid shopId, CancellationToken cancellationToken = default);
}
