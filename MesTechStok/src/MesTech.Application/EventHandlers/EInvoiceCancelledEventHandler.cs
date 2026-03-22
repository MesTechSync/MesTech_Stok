using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IEInvoiceCancelledEventHandler
{
    Task HandleAsync(Guid eInvoiceId, string ettnNo, string reason, CancellationToken ct);
}

public class EInvoiceCancelledEventHandler : IEInvoiceCancelledEventHandler
{
    private readonly ILogger<EInvoiceCancelledEventHandler> _logger;

    public EInvoiceCancelledEventHandler(ILogger<EInvoiceCancelledEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid eInvoiceId, string ettnNo, string reason, CancellationToken ct)
    {
        _logger.LogWarning(
            "E-Fatura iptal edildi — EInvoiceId={EInvoiceId}, ETTN={ETTN}, Reason={Reason}",
            eInvoiceId, ettnNo, reason);

        return Task.CompletedTask;
    }
}
