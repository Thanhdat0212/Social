using Domain.Common;
using Domain.Entities.Marketplace.Shops;

namespace Domain.Entities.Marketplace.Ledger;

/// <summary>
/// Bảng Read-Model lưu số dư hiện tại của gian hàng (được đồng bộ trong cùng transaction với LedgerService.Post)
/// Giúp truy vấn tức thời số dư mà không phải SUM toàn bộ bảng LedgerEntry.
/// </summary>
public class ShopBalanceCache : BaseEntity
{
    public Guid ShopId { get; private set; }
    public long PendingBalance { get; private set; }       // Tiền tạm giữ trong cửa sổ khiếu nại (VND)
    public long AvailableBalance { get; private set; }     // Tiền khả dụng có thể rút (VND)
    public long PayoutPendingBalance { get; private set; } // Tiền đang chờ xử lý giải ngân rút (VND)
    public long LifetimeGrossRevenue { get; private set; } // Tổng doanh thu tích lũy (VND)
    public long LifetimeCommissionPaid { get; private set; } // Tổng hoa hồng đã nộp cho sàn (VND)
    public int Version { get; private set; } = 1;

    // Navigation
    public Shop? Shop { get; private set; }

    protected ShopBalanceCache() { }

    public ShopBalanceCache(Guid shopId)
    {
        if (shopId == Guid.Empty) throw new ArgumentException("ShopId không hợp lệ.", nameof(shopId));
        ShopId = shopId;
        PendingBalance = 0;
        AvailableBalance = 0;
        PayoutPendingBalance = 0;
        LifetimeGrossRevenue = 0;
        LifetimeCommissionPaid = 0;
        Version = 1;
    }

    /// <summary>
    /// Ghi nhận phân bổ đơn hàng mới: tiền ròng vào Pending, cộng doanh thu và hoa hồng tích lũy
    /// </summary>
    public void ApplyOrderSettlement(long grossAmount, long commissionAmount, long netAmount)
    {
        if (grossAmount < 0 || commissionAmount < 0 || netAmount < 0)
            throw new ArgumentException("Số tiền không hợp lệ.");

        PendingBalance += netAmount;
        LifetimeGrossRevenue += grossAmount;
        LifetimeCommissionPaid += commissionAmount;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Giải phóng tiền từ Pending -> Available khi hết thời gian khiếu nại
    /// </summary>
    public void ReleasePendingToAvailable(long amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền giải phóng phải lớn hơn 0.");
        if (PendingBalance < amount)
            throw new InvalidOperationException($"Số dư Pending ({PendingBalance}) không đủ để giải phóng {amount}.");

        PendingBalance -= amount;
        AvailableBalance += amount;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Khóa tiền khả dụng khi Shop tạo yêu cầu rút tiền (Available -> PayoutPending)
    /// </summary>
    public void HoldPayout(long amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền rút phải lớn hơn 0.");
        if (AvailableBalance < amount)
            throw new InvalidOperationException($"Số dư khả dụng ({AvailableBalance}) không đủ để rút {amount}.");

        AvailableBalance -= amount;
        PayoutPendingBalance += amount;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Trừ dứt điểm tiền khi lệnh rút đã hoàn tất chuyển khoản thành công
    /// </summary>
    public void CompletePayout(long amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền phải lớn hơn 0.");
        if (PayoutPendingBalance < amount)
            throw new InvalidOperationException($"Số dư chờ rút ({PayoutPendingBalance}) nhỏ hơn số tiền hoàn tất {amount}.");

        PayoutPendingBalance -= amount;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Hoàn trả lại tiền vào số dư khả dụng khi lệnh rút tiền bị từ chối / thất bại
    /// </summary>
    public void RevertPayout(long amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền phải lớn hơn 0.");
        if (PayoutPendingBalance < amount)
            throw new InvalidOperationException($"Số dư chờ rút ({PayoutPendingBalance}) nhỏ hơn số tiền hoàn lại {amount}.");

        PayoutPendingBalance -= amount;
        AvailableBalance += amount;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Trừ tiền khi hoàn tiền đơn hàng (ưu tiên trừ từ Pending, nếu không đủ trừ Available)
    /// </summary>
    public void DeductRefund(long amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền hoàn phải lớn hơn 0.");

        if (PendingBalance >= amount)
        {
            PendingBalance -= amount;
        }
        else
        {
            var remaining = amount - PendingBalance;
            PendingBalance = 0;
            if (AvailableBalance < remaining)
                throw new InvalidOperationException("Tổng số dư của shop không đủ để hoàn tiền.");
            AvailableBalance -= remaining;
        }

        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đồng bộ lại số dư cache khớp chính xác với kết quả tổng hợp từ Sổ cái kép (Reconciliation)
    /// </summary>
    public void SyncFromLedger(long available, long pending, long payoutPending)
    {
        AvailableBalance = available;
        PendingBalance = pending;
        PayoutPendingBalance = payoutPending;
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
