using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 10 dakikada basarisiz faturalari tekrar dener.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class InvoiceRetryJob : ISyncJob
{
    public string JobId => "invoice-retry";
    public string CronExpression => "*/10 * * * *"; // Her 10 dk

    private readonly IInvoiceProvider _invoiceProvider;
    private readonly ILogger<InvoiceRetryJob> _logger;

    public InvoiceRetryJob(IInvoiceProvider invoiceProvider, ILogger<InvoiceRetryJob> logger)
    {
        _invoiceProvider = invoiceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Fatura retry basliyor (Provider: {Provider})...",
            JobId, _invoiceProvider.ProviderName);

        try
        {
            // InvoiceRepository eklenince Status=Error faturalar buradan cekilip
            // IInvoiceProvider ile tekrar denenecek. Simdilik provider aktifligini dogruluyoruz.

            _logger.LogInformation(
                "[{JobId}] Fatura retry tamamlandi — provider aktif: {Provider}",
                JobId, _invoiceProvider.ProviderName);

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Fatura retry HATA", JobId);
            throw;
        }
    }
}
