using Domain.Entities.Marketplace.Payments;
using Domain.Enums;

namespace Application.Interfaces.Repositories.Marketplace.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByProviderTxnIdAsync(PaymentProvider provider, string providerTxnId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    void Update(Payment payment);

    /// <summary>
    /// Đổi trạng thái sang Succeeded một cách nguyên tử (UPDATE ... WHERE Status chưa thành công).
    /// Trả về true nếu lời gọi này là lời gọi duy nhất đổi được trạng thái.
    /// </summary>
    Task<bool> TryMarkSucceededAsync(Guid paymentId, DateTime paidAtUtc, string? rawPayloadRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nạp lại giá trị mới nhất từ DB cho entity đang được track.
    /// </summary>
    Task ReloadAsync(Payment payment, CancellationToken cancellationToken = default);
}
