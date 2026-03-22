using MassTransit;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Filters;

public class IdempotencyFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IProcessedMessageStore _store;
    private readonly ILogger<IdempotencyFilter<T>> _logger;

    public IdempotencyFilter(
        IProcessedMessageStore store,
        ILogger<IdempotencyFilter<T>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageId = context.MessageId ?? Guid.NewGuid();

        if (await _store.IsProcessedAsync(messageId, context.CancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Duplicate message detected — skipping. MessageId: {MessageId}, Type: {Type}",
                messageId, typeof(T).Name);
            return;
        }

        await next.Send(context).ConfigureAwait(false);

        await _store.MarkProcessedAsync(
            messageId, typeof(T).Name, context.CancellationToken).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("idempotency");
}
