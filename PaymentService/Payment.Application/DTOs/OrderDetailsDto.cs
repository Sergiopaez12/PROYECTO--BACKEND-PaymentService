namespace Payment.Application.DTOs;

public record OrderItemDetailsDto(
    int ProductId,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal
);

public record OrderDetailsDto(
    int Id,
    int UserId,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDetailsDto> Items
);

