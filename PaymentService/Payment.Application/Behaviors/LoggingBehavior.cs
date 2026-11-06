using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Behaviors;

/// <summary>
/// Pipeline Behavior de MediatR que registra en logs el inicio, fin y duración de cada Command y Query.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[PaymentService] Iniciando manejo de {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("[PaymentService] Finalizado {RequestName} exitosamente en {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[PaymentService] Error al procesar {RequestName} tras {ElapsedMilliseconds} ms: {ErrorMessage}", requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
