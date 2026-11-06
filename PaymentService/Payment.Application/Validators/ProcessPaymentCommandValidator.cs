using FluentValidation;
using Payment.Application.Commands.ProcessPayment;

namespace Payment.Application.Validators;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("El número de orden (OrderId) debe ser mayor a 0.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("El método de pago es obligatorio.");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("El número de tarjeta es obligatorio.")
            .MinimumLength(13).WithMessage("El número de tarjeta debe tener al menos 13 dígitos.");
    }
}
