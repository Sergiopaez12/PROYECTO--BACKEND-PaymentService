using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    int OrderId,
    string PaymentMethod,
    string CardNumber = "",
    string ExpiryDate = "",
    string Cvv = ""
) : IRequest<PaymentResultDto>;
