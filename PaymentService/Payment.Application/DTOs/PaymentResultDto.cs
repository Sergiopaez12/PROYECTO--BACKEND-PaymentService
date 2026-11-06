namespace Payment.Application.DTOs;

public record PaymentResultDto(
    Guid TransactionId,
    int OrderId,
    int UserId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string TransactionReference,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? FailureReason,
    OrderDetailsDto? OrderDetails = null
);
