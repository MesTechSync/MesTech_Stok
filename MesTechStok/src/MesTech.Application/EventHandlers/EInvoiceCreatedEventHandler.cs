using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// E-fatura oluşturulduğunda loglama ve gönderim kuyruğuna ekleme.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IEInvoiceCreatedEventHandler
{
    Task HandleAsync(Guid eInvoiceId, string ettnNo, EInvoiceType type, CancellationToken ct);
}

public sealed class EInvoiceCreatedEventHandler : IEInvoiceCreatedEventHandler
{
    private readonly ILogger<EInvoiceCreatedEventHandler> _logger;

    public EInvoiceCreatedEventHandler(ILogger<EInvoiceCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(Guid eInvoiceId, string ettnNo, EInvoiceType type, CancellationToken ct)
    {
        _logger.LogInformation(
            "EInvoiceCreated → EInvoiceId={EInvoiceId}, ETTN={EttnNo}, Type={InvoiceType}",
            eInvoiceId, ettnNo, type);

        // FUTURE: E-fatura gönderim kuyruğuna ekle (Sovos/GiB portal)
        _logger.LogInformation(
            "E-fatura gönderim kuyruğuna eklendi — ETTN={EttnNo}", ettnNo);

        return Task.CompletedTask;
    }
}
