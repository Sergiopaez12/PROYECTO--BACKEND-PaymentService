using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<PaymentTransaction?> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.OrderId == orderId, ct);
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default)
    {
        await _context.Transactions.AddAsync(transaction, ct);
    }

    public Task UpdateAsync(PaymentTransaction transaction, CancellationToken ct = default)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
