using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 10 dakikada basarisiz faturalari tekrar dener.
/// </summary>
public class InvoiceRetryJob : ISyncJob
{
    public string JobId => "invoice-retry";
    public string CronExpression => "*/10 * * * *"; // Her 10 dk

    private readonly ILogger<InvoiceRetryJob> _logger;

    public InvoiceRetryJob(ILogger<InvoiceRetryJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Fatura retry basliyor...", JobId);

        // TODO: Status=Error olan faturalari tekrar IInvoiceProvider'a gonder

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Fatura retry tamamlandi", JobId);
    }
}
