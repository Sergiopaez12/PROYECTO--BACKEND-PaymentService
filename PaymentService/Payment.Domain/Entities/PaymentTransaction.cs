using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public int OrderId { get; private set; }
    public int UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string TransactionReference { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private PaymentTransaction() { }

    public PaymentTransaction(int orderId, int userId, decimal amount, string paymentMethod)
    {
        if (orderId <= 0)
            throw new ArgumentException("El ID de la orden debe ser mayor a cero.", nameof(orderId));
        if (userId <= 0)
            throw new ArgumentException("El ID de usuario debe ser mayor a cero.", nameof(userId));
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.", nameof(amount));

        Id = Guid.NewGuid();
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "CreditCard" : paymentMethod;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        TransactionReference = string.Empty;
    }

    public void Approve(string reference)
    {
        Status = PaymentStatus.Approved;
        TransactionReference = reference;
        ProcessedAt = DateTime.UtcNow;
        FailureReason = null;
    }

    public void Reject(string reason)
    {
        Status = PaymentStatus.Rejected;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Transacción rechazada" : reason;
        ProcessedAt = DateTime.UtcNow;
        TransactionReference = string.Empty;
    }
}
