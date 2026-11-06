using System.ComponentModel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands.ProcessPayment;
using Payment.Application.DTOs;
using Payment.Application.Queries.GetPaymentByOrderId;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// 1. Buscar pedido por PedidoId — Obtiene todos los detalles del pedido desde ECommerce y el estado de su pago.
    /// </summary>
    /// <param name="pedidoId">Número de ID del pedido generado en ECommerce (ej: 1, 2, 3...)</param>
    /// <param name="ct">Token de cancelación</param>
    [HttpGet("pedido/{pedidoId:int}")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResultDto>> GetByOrderId(
        int pedidoId,
        CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new GetPaymentByOrderIdQuery(pedidoId), ct);
            return result is null
                ? NotFound(new { message = $"No se encontró ningún pedido con el PedidoId #{pedidoId} en el servicio de ECommerce." })
                : Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "No se pudo conectar con el servicio de ECommerce (asegurate de que ECommerce.Api esté corriendo en http://localhost:5080).",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// 2. Efectuar el pago por PedidoId — Busca automáticamente el pedido y su monto total en ECommerce y procesa el pago.
    /// </summary>
    /// <param name="pedidoId">Número de ID del pedido generado en ECommerce (ej: 1, 2, 3...)</param>
    /// <param name="request">Datos del método de pago y tarjeta</param>
    /// <param name="ct">Token de cancelación</param>
    [HttpPost("pedido/{pedidoId:int}/pagar")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResultDto>> PayByPedidoId(
        int pedidoId,
        [FromBody] PayOrderRequest request,
        CancellationToken ct)
    {
        var command = new ProcessPaymentCommand(
            pedidoId,
            request.PaymentMethod,
            request.CardNumber,
            request.ExpiryDate,
            request.Cvv);

        return await ExecutePaymentAsync(command, ct);
    }

    /// <summary>
    /// Procesar pago enviando el PedidoId (OrderId) dentro del cuerpo JSON.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResultDto>> ProcessPayment(
        [FromBody] ProcessPaymentCommand command,
        CancellationToken ct)
    {
        return await ExecutePaymentAsync(command, ct);
    }

    private async Task<ActionResult<PaymentResultDto>> ExecutePaymentAsync(
        ProcessPaymentCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors.Select(e => new { property = e.PropertyName, error = e.ErrorMessage });
            return BadRequest(new { message = "Error de validación en la solicitud de pago.", errors });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "No se pudo conectar con el servicio de ECommerce (asegurate de que ECommerce.Api esté corriendo en http://localhost:5080).",
                detail = ex.Message
            });
        }
    }
}

public record PayOrderRequest(
    [property: DefaultValue("CreditCard")] string PaymentMethod = "CreditCard",
    [property: DefaultValue("4532111122223333")] string CardNumber = "4532111122223333",
    [property: DefaultValue("12/28")] string ExpiryDate = "12/28",
    [property: DefaultValue("123")] string Cvv = "123"
);
