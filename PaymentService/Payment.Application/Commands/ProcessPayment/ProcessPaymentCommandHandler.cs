using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Application.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResultDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IOrderServiceClient _orderServiceClient;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentProcessor paymentProcessor,
        IOrderServiceClient orderServiceClient)
    {
        _paymentRepository = paymentRepository;
        _paymentProcessor = paymentProcessor;
        _orderServiceClient = orderServiceClient;
    }

    public async Task<PaymentResultDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar la orden en el microservicio de ECommerce por su número de OrderId
        var orderDetails = await _orderServiceClient.GetOrderByIdAsync(request.OrderId, cancellationToken);
        if (orderDetails is null)
        {
            throw new KeyNotFoundException($"No se encontró la orden #{request.OrderId} en el servicio de ECommerce.");
        }

        // 2. Procesar el pago con la pasarela (MockPaymentProcessor)
        var (isSuccess, reference, failureReason) = _paymentProcessor.Process(request.CardNumber, orderDetails.Total);

        // 3. Si ya existe registro para esta orden, actualizar su resultado de pago
        var existing = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            if (isSuccess)
            {
                existing.Approve(reference);
                await _orderServiceClient.ConfirmOrderInECommerceAsync(request.OrderId, cancellationToken);
                orderDetails = orderDetails with { Status = "Aprobado" };
            }
            else
            {
                existing.Reject(failureReason ?? "Pago rechazado por la pasarela de pagos.");
                orderDetails = orderDetails with { Status = "Rechazado" };
            }

            await _paymentRepository.UpdateAsync(existing, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            return MapToDto(existing, orderDetails);
        }

        // 4. Instanciar nueva transacción de dominio tomando UserId y Total directamente de la orden encontrada
        var transaction = new PaymentTransaction(
            orderDetails.Id,
            orderDetails.UserId,
            orderDetails.Total,
            request.PaymentMethod);

        if (isSuccess)
        {
            transaction.Approve(reference);
            await _orderServiceClient.ConfirmOrderInECommerceAsync(request.OrderId, cancellationToken);
            orderDetails = orderDetails with { Status = "Aprobado" };
        }
        else
        {
            transaction.Reject(failureReason ?? "Pago rechazado por la pasarela de pagos.");
            orderDetails = orderDetails with { Status = "Rechazado" };
        }

        // 5. Persistir en la base de datos
        await _paymentRepository.AddAsync(transaction, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(transaction, orderDetails);
    }

    internal static string MapStatusToSpanish(PaymentStatus status) => status switch
    {
        PaymentStatus.Approved => "Aprobado",
        PaymentStatus.Rejected => "Rechazado",
        _ => "Pendiente"
    };

    private static PaymentResultDto MapToDto(PaymentTransaction t, OrderDetailsDto orderDetails) => new(
        t.Id,
        t.OrderId,
        t.UserId,
        t.Amount,
        t.PaymentMethod,
        MapStatusToSpanish(t.Status),
        t.TransactionReference,
        t.CreatedAt,
        t.ProcessedAt,
        t.FailureReason,
        orderDetails
    );
}
