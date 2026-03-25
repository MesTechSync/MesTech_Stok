using System.Diagnostics;
using MediatR;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — tüm command/query'leri loglar.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            // Loglama Infrastructure katmanında inject edilecek
            return response;
        }
        catch (Exception)
        {
            sw.Stop();
            throw;
        }
    }
}
