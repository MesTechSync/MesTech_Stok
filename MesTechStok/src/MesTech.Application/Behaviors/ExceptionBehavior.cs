using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — unhandled exception'ları yapısal olarak loglar.
/// Handler'larda try-catch yazmaya gerek kalmaz; pipeline otomatik yakalar.
/// Exception yutmaz, yeniden fırlatır (rethrow).
/// </summary>
public class ExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ExceptionBehavior<TRequest, TResponse>> _logger;

    public ExceptionBehavior(ILogger<ExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(ex,
                "Unhandled exception in handler {RequestName}: {ExceptionType} — {Message}",
                requestName, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}
