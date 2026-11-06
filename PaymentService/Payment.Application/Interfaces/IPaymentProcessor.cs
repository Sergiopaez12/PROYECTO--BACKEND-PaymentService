namespace Payment.Application.Interfaces;

public interface IPaymentProcessor
{
    (bool IsSuccess, string Reference, string? FailureReason) Process(string cardNumber, decimal amount);
}
