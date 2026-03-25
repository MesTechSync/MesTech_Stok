using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IInvoiceGeneratedForERPEventHandler
{
    Task HandleAsync(InvoiceGeneratedForERPEvent domainEvent, CancellationToken ct);
}

public sealed class InvoiceGeneratedForERPEventHandler : IInvoiceGeneratedForERPEventHandler
{
    private readonly ILogger<InvoiceGeneratedForERPEventHandler> _logger;

    public InvoiceGeneratedForERPEventHandler(ILogger<InvoiceGeneratedForERPEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(InvoiceGeneratedForERPEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Fatura ERP sync tetiklendi — InvoiceId={InvoiceId}, TargetERP={ERP}, Amount={Amount}",
            domainEvent.InvoiceId, domainEvent.TargetERP, domainEvent.TotalAmount);
        return Task.CompletedTask;
    }
}
