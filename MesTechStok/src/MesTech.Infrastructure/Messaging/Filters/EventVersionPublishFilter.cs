using MassTransit;

namespace MesTech.Infrastructure.Messaging.Filters;

/// <summary>
/// Publish filter — adds X-Event-Version header to all outbound messages.
/// Currently V1. When breaking changes occur, V2 exchange transition is used.
/// </summary>
public class EventVersionPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        context.Headers.Set("X-Event-Version", "1");
        context.Headers.Set("X-Source-System", "MesTech");
        context.Headers.Set("X-Correlation-Id",
            context.CorrelationId?.ToString() ?? Guid.NewGuid().ToString());
        return next.Send(context);
    }

    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("event-version");
}
