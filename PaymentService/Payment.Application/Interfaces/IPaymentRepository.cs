using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentTransaction?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);
    Task UpdateAsync(PaymentTransaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
