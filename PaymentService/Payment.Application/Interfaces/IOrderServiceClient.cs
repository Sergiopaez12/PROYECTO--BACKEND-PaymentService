using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IOrderServiceClient
{
    Task<OrderDetailsDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
    Task ConfirmOrderInECommerceAsync(int orderId, CancellationToken ct = default);
}
