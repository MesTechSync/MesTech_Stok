using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IInvoiceApprovedEventHandler
{
    Task HandleAsync(InvoiceApprovedEvent domainEvent, CancellationToken ct);
}

public sealed class InvoiceApprovedEventHandler : IInvoiceApprovedEventHandler
{
    private readonly ILogger<InvoiceApprovedEventHandler> _logger;

    public InvoiceApprovedEventHandler(ILogger<InvoiceApprovedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(InvoiceApprovedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "InvoiceApproved: InvoiceId={InvoiceId}, Tenant={TenantId}, Number={Number}, Total={Total}, Type={Type}",
            domainEvent.InvoiceId, domainEvent.TenantId, domainEvent.InvoiceNumber,
            domainEvent.GrandTotal, domainEvent.Type);

        return Task.CompletedTask;
    }
}
