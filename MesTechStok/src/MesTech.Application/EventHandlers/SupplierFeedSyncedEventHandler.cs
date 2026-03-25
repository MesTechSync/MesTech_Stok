using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ISupplierFeedSyncedEventHandler
{
    Task HandleAsync(SupplierFeedSyncedEvent domainEvent, CancellationToken ct);
}

public sealed class SupplierFeedSyncedEventHandler : ISupplierFeedSyncedEventHandler
{
    private readonly ILogger<SupplierFeedSyncedEventHandler> _logger;

    public SupplierFeedSyncedEventHandler(ILogger<SupplierFeedSyncedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(SupplierFeedSyncedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "SupplierFeedSynced: FeedId={FeedId}, SupplierId={SupplierId}, Total={Total}, Updated={Updated}, Deactivated={Deactivated}, Status={Status}",
            domainEvent.SupplierFeedId, domainEvent.SupplierId, domainEvent.TotalProducts,
            domainEvent.UpdatedProducts, domainEvent.DeactivatedProducts, domainEvent.Status);

        return Task.CompletedTask;
    }
}
