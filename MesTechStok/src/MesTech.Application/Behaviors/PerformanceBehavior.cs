using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — 500ms+ süren handler'ları loglar.
/// Yavaş query/command'ları Seq'te filtreleyerek tespit etmeyi sağlar.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var response = await next().ConfigureAwait(false);

        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Slow handler detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName, sw.ElapsedMilliseconds, SlowThresholdMs);
        }

        return response;
    }
}
