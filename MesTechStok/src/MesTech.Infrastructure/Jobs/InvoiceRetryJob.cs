using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 10 dakikada başarısız faturaları tekrar dener.
/// Status=Error veya ParasutSyncStatus=Failed olanları alır, IInvoiceProvider ile tekrar gönderir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class InvoiceRetryJob : ISyncJob
{
    public string JobId => "invoice-retry";
    public string CronExpression => "*/10 * * * *"; // Her 10 dk

    private readonly IInvoiceProvider _invoiceProvider;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceRetryJob> _logger;

    public InvoiceRetryJob(
        IInvoiceProvider invoiceProvider,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<InvoiceRetryJob> logger)
    {
        _invoiceProvider = invoiceProvider;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Fatura retry başlıyor (Provider: {Provider})...",
            JobId, _invoiceProvider.ProviderName);

        try
        {
            var failedInvoices = await _invoiceRepository.GetFailedAsync(50, ct).ConfigureAwait(false);

            if (failedInvoices.Count == 0)
            {
                _logger.LogDebug("[{JobId}] Başarısız fatura yok — retry atlanıyor", JobId);
                return;
            }

            _logger.LogInformation("[{JobId}] {Count} başarısız fatura retry edilecek", JobId, failedInvoices.Count);

            int success = 0, failed = 0;

            foreach (var invoice in failedInvoices)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // GibInvoiceId varsa status kontrol et — belki gönderilmiş ama response alınamamış
                    if (!string.IsNullOrEmpty(invoice.GibInvoiceId))
                    {
                        var status = await _invoiceProvider.CheckStatusAsync(invoice.GibInvoiceId, ct).ConfigureAwait(false);
                        if (status.Status is "Accepted" or "Sent")
                        {
                            invoice.MarkAsSent(invoice.GibInvoiceId, null);
                            success++;
                            continue;
                        }
                    }

                    // GibInvoiceId yoksa henüz gönderilmemiş — manuel gönderim gerekli
                    _logger.LogWarning("[{JobId}] Fatura {InvoiceId} GIB ID yok — yeniden gönderim gerekli (InvoiceNumber={Num})",
                        JobId, invoice.Id, invoice.InvoiceNumber);
                    failed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{JobId}] Fatura {InvoiceId} retry exception", JobId, invoice.Id);
                    failed++;
                }
            }

            if (success > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Fatura retry tamamlandı — success={Success}, failed={Failed}, total={Total}",
                JobId, success, failed, failedInvoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Fatura retry HATA", JobId);
            throw;
        }
    }
}
