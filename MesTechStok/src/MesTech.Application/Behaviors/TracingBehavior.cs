using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR tracing behavior — her handler çağrısında Activity span oluşturur.
/// W3C Trace-Context uyumlu. Serilog + Seq + OpenTelemetry ile entegre.
///
/// Span attributes:
/// - mestech.handler.name: handler sınıf adı
/// - mestech.handler.duration_ms: süre
/// - mestech.handler.success: başarılı mı
/// - mestech.handler.exception: hata mesajı (varsa)
///
/// DEV6-TUR15: Distributed tracing derinleştirme.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("MesTech.Application.Handlers");
    private readonly ILogger<TracingBehavior<TRequest, TResponse>> _logger;

    public TracingBehavior(ILogger<TracingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;

        using var activity = ActivitySource.StartActivity(
            $"Handler:{handlerName}",
            ActivityKind.Internal,
            Activity.Current?.Context ?? default);

        if (activity is not null)
        {
            activity.SetTag("mestech.handler.name", handlerName);
            activity.SetTag("mestech.handler.request_type", typeof(TRequest).FullName);
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            sw.Stop();
            activity?.SetTag("mestech.handler.duration_ms", sw.ElapsedMilliseconds);
            activity?.SetTag("mestech.handler.success", true);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogDebug(
                "Handler {HandlerName} completed in {DurationMs}ms, TraceId={TraceId}",
                handlerName, sw.ElapsedMilliseconds,
                activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? "none");

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetTag("mestech.handler.duration_ms", sw.ElapsedMilliseconds);
            activity?.SetTag("mestech.handler.success", false);
            activity?.SetTag("mestech.handler.exception", ex.GetType().Name);
            activity?.SetTag("mestech.handler.error_message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex,
                "Handler {HandlerName} FAILED after {DurationMs}ms, TraceId={TraceId}",
                handlerName, sw.ElapsedMilliseconds,
                activity?.TraceId.ToString() ?? "none");

            throw;
        }
    }
}
