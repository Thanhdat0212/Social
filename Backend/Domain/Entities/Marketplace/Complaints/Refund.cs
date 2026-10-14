using Domain.Common;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;

namespace Domain.Entities.Marketplace.Complaints;

/// <summary>
/// Thực thể Hoàn tiền (Refund) - Nghĩa vụ hoàn trả tiền khi khiếu nại thành công hoặc lỗi hệ thống
/// </summary>
public class Refund : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public Guid ShopOrderId { get; private set; }
    public Guid? OrderItemId { get; private set; }
    public Guid? ComplaintId { get; private set; }

    public long Amount { get; private set; }
    public RefundBearer Bearer { get; private set; } = RefundBearer.Shop;
    public long ShopDeductionAmount { get; private set; }
    public long PlatformDeductionAmount { get; private set; }

    public RefundStatus Status { get; private set; } = RefundStatus.Pending;
    public string? ReasonCode { get; private set; }
    public string? ProviderRefundId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    // Navigation
    public Payment? Payment { get; private set; }
    public ShopOrder? ShopOrder { get; private set; }
    public OrderItem? OrderItem { get; private set; }
    public Complaint? Complaint { get; private set; }

    protected Refund() { }

    public Refund(
        Guid paymentId,
        Guid shopOrderId,
        long amount,
        RefundBearer bearer,
        long shopDeductionAmount,
        long platformDeductionAmount,
        Guid? orderItemId = null,
        Guid? complaintId = null,
        string? reasonCode = null)
    {
        if (paymentId == Guid.Empty) throw new ArgumentException("PaymentId không hợp lệ.", nameof(paymentId));
        if (shopOrderId == Guid.Empty) throw new ArgumentException("ShopOrderId không hợp lệ.", nameof(shopOrderId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền hoàn phải lớn hơn 0.");
        if (shopDeductionAmount + platformDeductionAmount != amount)
        {
            throw new InvalidOperationException($"Tổng tiền chịu trách nhiệm ({shopDeductionAmount} + {platformDeductionAmount}) phải bằng tổng tiền hoàn ({amount}).");
        }

        PaymentId = paymentId;
        ShopOrderId = shopOrderId;
        OrderItemId = orderItemId;
        ComplaintId = complaintId;
        Amount = amount;
        Bearer = bearer;
        ShopDeductionAmount = shopDeductionAmount;
        PlatformDeductionAmount = platformDeductionAmount;
        Status = RefundStatus.Pending;
        ReasonCode = reasonCode?.Trim();
    }

    public void MarkProcessing()
    {
        Status = RefundStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSucceeded(string? providerRefundId = null)
    {
        Status = RefundStatus.Succeeded;
        ProviderRefundId = providerRefundId?.Trim();
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RefundStatus.Failed;
        FailureReason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
