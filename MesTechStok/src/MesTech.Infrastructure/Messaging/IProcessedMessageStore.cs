namespace MesTech.Infrastructure.Messaging;

public interface IProcessedMessageStore
{
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid messageId, string consumerName, CancellationToken ct = default);
}
