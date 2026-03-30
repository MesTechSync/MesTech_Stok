using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// Ba/Bs Form rapor servisi.
/// Ba formu: Aylik 5.000 TL ustu alislar — tedarikci bazli.
/// Bs formu: Aylik 5.000 TL ustu satislar — musteri bazli.
/// VUK 396 Sira No'lu Genel Teblig.
///
/// AccountingDocument entity'si uzerinden calisir:
///   - DocumentType.PurchaseInvoice → Ba formu (alislar)
///   - DocumentType.SalesInvoice → Bs formu (satislar)
///   - CounterpartyId ile karsi taraf eslesmesi
/// </summary>
public sealed class BaBsReportService : IBaBsReportService
{
    /// <summary>
    /// Ba/Bs formu icin minimum tutar esigi (TL).
    /// VUK 396: 5.000 TL ve uzeri islemler bildirilir.
    /// </summary>
    public const decimal MinimumThreshold = 5_000m;

    private readonly IAccountingDocumentRepository _docRepo;
    private readonly ICounterpartyRepository _counterpartyRepo;
    private readonly ILogger<BaBsReportService> _logger;

    public BaBsReportService(
        IAccountingDocumentRepository docRepo,
        ICounterpartyRepository counterpartyRepo,
        ILogger<BaBsReportService> logger)
    {
        _docRepo = docRepo ?? throw new ArgumentNullException(nameof(docRepo));
        _counterpartyRepo = counterpartyRepo ?? throw new ArgumentNullException(nameof(counterpartyRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BaBsReportDto> GenerateBaBsReportAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Ay 1-12 araliginda olmalidir.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Yil 2000-2100 araliginda olmalidir.");

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        // Fetch purchase invoices (Ba)
        var purchaseDocs = await _docRepo.GetByTypeAsync(tenantId, DocumentType.PurchaseInvoice, ct);
        var purchaseInPeriod = purchaseDocs
            .Where(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate
                && d.CounterpartyId.HasValue && d.Amount.HasValue)
            .ToList();

        // Fetch sales invoices (Bs)
        var salesDocs = await _docRepo.GetByTypeAsync(tenantId, DocumentType.SalesInvoice, ct);
        var salesInPeriod = salesDocs
            .Where(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate
                && d.CounterpartyId.HasValue && d.Amount.HasValue)
            .ToList();

        // Load counterparties
        var counterparties = await _counterpartyRepo.GetAllAsync(tenantId, ct: ct);
        var cpMap = counterparties.ToDictionary(c => c.Id, c => c);

        var report = new BaBsReportDto
        {
            TenantId = tenantId,
            Year = year,
            Month = month
        };

        // Ba: Group purchases by counterparty, filter >= 5.000 TL
        var baGroups = purchaseInPeriod
            .GroupBy(d => d.CounterpartyId!.Value)
            .Select(g => new
            {
                CounterpartyId = g.Key,
                TotalAmount = g.Sum(d => d.Amount!.Value),
                DocumentCount = g.Count()
            })
            .Where(g => g.TotalAmount >= MinimumThreshold);

        foreach (var group in baGroups)
        {
            if (cpMap.TryGetValue(group.CounterpartyId, out var cp))
            {
                report.BaEntries.Add(new BaBsCounterpartyDto
                {
                    Name = cp.Name,
                    VKN = cp.VKN ?? string.Empty,
                    TotalAmount = Math.Round(group.TotalAmount, 2),
                    DocumentCount = group.DocumentCount
                });
            }
        }

        // Bs: Group sales by counterparty, filter >= 5.000 TL
        var bsGroups = salesInPeriod
            .GroupBy(d => d.CounterpartyId!.Value)
            .Select(g => new
            {
                CounterpartyId = g.Key,
                TotalAmount = g.Sum(d => d.Amount!.Value),
                DocumentCount = g.Count()
            })
            .Where(g => g.TotalAmount >= MinimumThreshold);

        foreach (var group in bsGroups)
        {
            if (cpMap.TryGetValue(group.CounterpartyId, out var cp))
            {
                report.BsEntries.Add(new BaBsCounterpartyDto
                {
                    Name = cp.Name,
                    VKN = cp.VKN ?? string.Empty,
                    TotalAmount = Math.Round(group.TotalAmount, 2),
                    DocumentCount = group.DocumentCount
                });
            }
        }

        _logger.LogInformation(
            "BaBs raporu olusturuldu — Tenant={TenantId}, {Year}/{Month:D2}, Ba={BaCount}, Bs={BsCount}",
            tenantId, year, month, report.BaEntries.Count, report.BsEntries.Count);

        return report;
    }
}
