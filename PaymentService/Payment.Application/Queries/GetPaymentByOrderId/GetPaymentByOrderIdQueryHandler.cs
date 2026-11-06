using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Queries.GetPaymentByOrderId;

public class GetPaymentByOrderIdQueryHandler : IRequestHandler<GetPaymentByOrderIdQuery, PaymentResultDto?>
{
    private readonly IOrderServiceClient _orderServiceClient;

    public GetPaymentByOrderIdQueryHandler(IOrderServiceClient orderServiceClient)
    {
        _orderServiceClient = orderServiceClient;
    }

    public async Task<PaymentResultDto?> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var orderDetails = await _orderServiceClient.GetOrderByIdAsync(request.OrderId, cancellationToken);
        if (orderDetails is null)
            return null;

        // En la consulta (GET) por PedidoId se muestran todos los detalles de la orden en estado "Pendiente" previo a efectuar el pago
        return new PaymentResultDto(
            Guid.Empty,
            orderDetails.Id,
            orderDetails.UserId,
            orderDetails.Total,
            "Pendiente de pago",
            "Pendiente",
            string.Empty,
            orderDetails.CreatedAt,
            null,
            null,
            orderDetails with { Status = "Pendiente" }
        );
    }
}
