using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IDailySummaryGeneratedEventHandler
{
    Task HandleAsync(DailySummaryGeneratedEvent domainEvent, CancellationToken ct);
}

public sealed class DailySummaryGeneratedEventHandler : IDailySummaryGeneratedEventHandler
{
    private readonly ILogger<DailySummaryGeneratedEventHandler> _logger;

    public DailySummaryGeneratedEventHandler(ILogger<DailySummaryGeneratedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DailySummaryGeneratedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "DailySummary: Date={Date}, Orders={Orders}, Revenue={Revenue}, StockAlerts={Alerts}, Invoices={Invoices}",
            domainEvent.Date, domainEvent.OrderCount, domainEvent.Revenue,
            domainEvent.StockAlerts, domainEvent.InvoiceCount);

        return Task.CompletedTask;
    }
}
