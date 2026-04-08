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
[DisableConcurrentExecution(timeoutInSeconds: 300)]
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

                    // GibInvoiceId yoksa henüz gönderilmemiş — tekrar gönder
                    _logger.LogInformation("[{JobId}] Fatura {InvoiceId} GIB ID yok — yeniden gönderiliyor (InvoiceNumber={Num})",
                        JobId, invoice.Id, invoice.InvoiceNumber);

                    // VKN sorgusu → e-Fatura veya e-Arşiv karar
                    var useEFatura = false;
                    if (!string.IsNullOrEmpty(invoice.CustomerTaxNumber) && invoice.CustomerTaxNumber.Length >= 10)
                    {
                        try
                        {
                            useEFatura = await _invoiceProvider.IsEInvoiceTaxpayerAsync(
                                invoice.CustomerTaxNumber, ct).ConfigureAwait(false);
                        }
                        catch (Exception vknEx) when (vknEx is not OperationCanceledException)
                        {
                            _logger.LogWarning(vknEx, "[{JobId}] VKN sorgu başarısız {VKN} — e-Arşiv ile devam", JobId, invoice.CustomerTaxNumber);
                        }
                    }

                    var dto = new InvoiceDto(
                        InvoiceNumber: invoice.InvoiceNumber ?? $"INV-{invoice.Id:N}"[..16],
                        CustomerName: invoice.CustomerName,
                        CustomerTaxNumber: invoice.CustomerTaxNumber,
                        CustomerTaxOffice: invoice.CustomerTaxOffice,
                        CustomerAddress: invoice.CustomerAddress,
                        SubTotal: invoice.SubTotal,
                        TaxTotal: invoice.TaxTotal,
                        GrandTotal: invoice.GrandTotal,
                        Lines: invoice.Lines.Select(l => new InvoiceLineDto(
                            l.ProductName, l.SKU, l.Quantity, l.UnitPrice,
                            l.TaxRate, l.TaxAmount, l.LineTotal)).ToList());

                    var result = useEFatura
                        ? await _invoiceProvider.CreateEFaturaAsync(dto, ct).ConfigureAwait(false)
                        : await _invoiceProvider.CreateEArsivAsync(dto, ct).ConfigureAwait(false);

                    if (result.Success)
                    {
                        invoice.MarkAsSent(result.GibInvoiceId, result.PdfUrl);
                        success++;
                        _logger.LogInformation("[{JobId}] Fatura {InvoiceId} retry BAŞARILI — GIB={GibId}",
                            JobId, invoice.Id, result.GibInvoiceId);
                    }
                    else
                    {
                        _logger.LogWarning("[{JobId}] Fatura {InvoiceId} retry BAŞARISIZ — {Error}",
                            JobId, invoice.Id, result.ErrorMessage);
                        failed++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Fatura retry HATA", JobId);
            throw;
        }
    }
}
