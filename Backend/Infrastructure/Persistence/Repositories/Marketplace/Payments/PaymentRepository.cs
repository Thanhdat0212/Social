using Application.Interfaces.Repositories.Marketplace.Payments;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Marketplace.Payments;

public class PaymentRepository : IPaymentRepository
{
    private readonly SocialDbContext _dbContext;

    public PaymentRepository(SocialDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Checkout)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Checkout)
            .FirstOrDefaultAsync(p => p.CheckoutId == checkoutId, cancellationToken);
    }

    public async Task<Payment?> GetByProviderTxnIdAsync(PaymentProvider provider, string providerTxnId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Checkout)
            .FirstOrDefaultAsync(p => p.Provider == provider && p.ProviderTxnId == providerTxnId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public void Update(Payment payment)
    {
        _dbContext.Payments.Update(payment);
    }

    public async Task<bool> TryMarkSucceededAsync(Guid paymentId, DateTime paidAtUtc, string? rawPayloadRef, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        // Câu UPDATE có điều kiện: nếu 2 request chạy song song, request thứ 2 sẽ chờ khóa dòng
        // rồi đánh giá lại WHERE -> 0 dòng. Nhờ vậy chỉ đúng 1 request ghi nhận tiền vào sổ cái.
        var affected = await _dbContext.Payments
            .Where(p => p.Id == paymentId &&
                        (p.Status == PaymentStatus.Initiated ||
                         p.Status == PaymentStatus.Expired ||
                         p.Status == PaymentStatus.Failed))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Succeeded)
                .SetProperty(p => p.PaidAt, (DateTime?)paidAtUtc)
                .SetProperty(p => p.RawPayloadRef, p => rawPayloadRef ?? p.RawPayloadRef)
                .SetProperty(p => p.UpdatedAtUtc, nowUtc),
                cancellationToken);

        return affected == 1;
    }

    public async Task ReloadAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Entry(payment).ReloadAsync(cancellationToken);
    }
}
