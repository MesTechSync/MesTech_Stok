using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IEInvoiceSentEventHandler
{
    Task HandleAsync(Guid eInvoiceId, string ettnNo, string? providerRef, CancellationToken ct);
}

public class EInvoiceSentEventHandler : IEInvoiceSentEventHandler
{
    private readonly ILogger<EInvoiceSentEventHandler> _logger;

    public EInvoiceSentEventHandler(ILogger<EInvoiceSentEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid eInvoiceId, string ettnNo, string? providerRef, CancellationToken ct)
    {
        _logger.LogInformation(
            "E-Fatura gönderildi — EInvoiceId={EInvoiceId}, ETTN={ETTN}, ProviderRef={ProviderRef}",
            eInvoiceId, ettnNo, providerRef);

        return Task.CompletedTask;
    }
}
