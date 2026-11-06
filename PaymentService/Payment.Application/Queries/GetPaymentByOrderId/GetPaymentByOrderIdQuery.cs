using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Queries.GetPaymentByOrderId;

public record GetPaymentByOrderIdQuery(int OrderId) : IRequest<PaymentResultDto?>;
