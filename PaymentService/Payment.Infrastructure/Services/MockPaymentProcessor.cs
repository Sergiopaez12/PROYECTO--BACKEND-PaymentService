using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

public class MockPaymentProcessor : IPaymentProcessor
{
    public (bool IsSuccess, string Reference, string? FailureReason) Process(string cardNumber, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, string.Empty, "El monto debe ser mayor a cero.");
        }

        var cleanCard = cardNumber?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;

        // Regla de Negocio: Tarjetas terminadas en 0000 son rechazadas por la pasarela
        if (cleanCard.EndsWith("0000"))
        {
            return (false, string.Empty, "Tarjeta rechazada por la pasarela de prueba (0000)");
        }

        // Pago aprobado: Generar código de referencia único
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var reference = $"MOCK-PAY-{uniqueSuffix}";

        return (true, reference, null);
    }
}
