using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.ERP;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: Her 15 dakika Pending faturalarını Paraşüt'e sync eder.
/// Max 10 fatura/çalışma (rate limit koruması).
/// Feature flag: Parasut.InvoiceSyncEnabled
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class ParasutInvoiceSyncJob
{
    private readonly AppDbContext _db;
    private readonly IParasutInvoiceSyncService _syncService;
    private readonly ParasutOptions _options;
    private readonly ILogger<ParasutInvoiceSyncJob> _logger;
    private const int BatchSize = 10;

    public ParasutInvoiceSyncJob(
        AppDbContext db,
        IParasutInvoiceSyncService syncService,
        IOptions<ParasutOptions> options,
        ILogger<ParasutInvoiceSyncJob> logger)
    {
        _db = db;
        _syncService = syncService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!_options.InvoiceSyncEnabled)
        {
            _logger.LogDebug("Paraşüt invoice sync disabled");
            return;
        }

        var pendingInvoices = await _db.Set<Domain.Entities.Invoice>()
            .Where(i => (i.ParasutSyncStatus == null || i.ParasutSyncStatus == SyncStatus.PendingSync)
                     && (i.Type == InvoiceType.EFatura || i.Type == InvoiceType.EArsiv)
                     && i.CreatedAt > DateTime.UtcNow.AddDays(-7)
                     && i.Status != InvoiceStatus.Cancelled)
            .OrderBy(i => i.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        _logger.LogInformation("Paraşüt sync: {Count} pending invoices", pendingInvoices.Count);

        foreach (var invoice in pendingInvoices)
        {
            try
            {
                var request = new ParasutInvoiceRequest(
                    invoice.InvoiceNumber,
                    $"Fatura {invoice.InvoiceNumber}",
                    invoice.InvoiceDate,
                    null,
                    invoice.Currency,
                    invoice.IsEInvoiceTaxpayer,
                    new List<ParasutInvoiceLineRequest>());

                var salesId = await _syncService.CreateSalesInvoiceAsync(request, ct);
                if (salesId == null)
                {
                    invoice.MarkParasutFailed("Sales invoice creation failed");
                    continue;
                }

                // e-Fatura mükellefi → e-Fatura, değilse → e-Arşiv
                var eId = invoice.IsEInvoiceTaxpayer
                    ? await _syncService.CreateEInvoiceAsync(salesId, ct)
                    : await _syncService.CreateEArchiveAsync(salesId, ct);

                invoice.MarkParasutSynced(salesId, eId);

                _logger.LogInformation("Synced invoice {Number} → Paraşüt {SalesId}", invoice.InvoiceNumber, salesId);
            }
            catch (Exception ex)
            {
                invoice.MarkParasutFailed(ex.Message);
                _logger.LogError(ex, "Paraşüt sync failed for {Number}", invoice.InvoiceNumber);
            }
        }

        if (pendingInvoices.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}
