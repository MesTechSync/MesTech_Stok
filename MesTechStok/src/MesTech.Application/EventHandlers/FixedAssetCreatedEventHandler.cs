using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IFixedAssetCreatedEventHandler
{
    Task HandleAsync(FixedAssetCreatedEvent domainEvent, CancellationToken ct);
}

public class FixedAssetCreatedEventHandler : IFixedAssetCreatedEventHandler
{
    private readonly ILogger<FixedAssetCreatedEventHandler> _logger;

    public FixedAssetCreatedEventHandler(ILogger<FixedAssetCreatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(FixedAssetCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sabit kıymet oluştu — AssetId={Id}, Name={Name}, Cost={Cost}, Method={Method}",
            domainEvent.FixedAssetId, domainEvent.AssetName, domainEvent.AcquisitionCost, domainEvent.Method);
        return Task.CompletedTask;
    }
}
