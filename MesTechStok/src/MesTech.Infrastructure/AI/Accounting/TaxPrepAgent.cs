using System.Globalization;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// Aylik vergi taslagi hazirlama arayuzu.
/// Hesaplanan KDV (391), Indirilecek KDV (191), tevkifat ve stopaj toplamlarini
/// JournalEntry + TaxWithholding verilerinden hesaplar.
/// UYARI: Sonuc her zaman TASLAK niteliktedir — kesin beyanname mali musavir tarafindan hazirlanmalidir.
/// </summary>
public interface ITaxPrepAgent
{
    Task<TaxPrepReport> PrepareMonthlyTaxAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
}

/// <summary>
/// Aylik vergi taslak raporu.
/// </summary>
public record TaxPrepReport(
    int Year,
    int Month,
    decimal TotalSales,
    decimal TotalPurchases,
    decimal CalculatedVAT,
    decimal DeductibleVAT,
    decimal PayableVAT,
    decimal TotalWithholding,
    decimal TotalStopaj,
    string Disclaimer,
    IReadOnlyList<TaxLineItem> Details);

public record TaxLineItem(string Description, string AccountCode, decimal Amount);

/// <summary>
/// TaxPrepAgent — JournalEntry ve TaxWithholding verilerinden
/// aylik KDV beyanname taslagi olusturur.
///
/// Hesap kodlari (Tekduzen Hesap Plani):
///   391 — Hesaplanan KDV (satislardan alinan KDV)
///   191 — Indirilecek KDV (alislardan odenen KDV)
///   360.01 — Odenecek KDV
///   360.02 — Odenecek Gelir Vergisi Stopaji
///
/// Odenecek KDV = 391 - 191
/// Sonuc negatifse → Devreden KDV (190 hesabi)
/// </summary>
public sealed class TaxPrepAgent : ITaxPrepAgent
{
    private const string TaxDisclaimer =
        "Bu bir TASLAK rapordur. Kesin beyanname mali musavir tarafindan hazirlanmalidir.";

    /// <summary>Hesaplanan KDV hesap kodu (satislardan alinan).</summary>
    private const string AccountCode391 = "391";

    /// <summary>Indirilecek KDV hesap kodu (alislardan odenen).</summary>
    private const string AccountCode191 = "191";

    /// <summary>Odenecek Gelir Vergisi Stopaji.</summary>
    private const string AccountCode360_02 = "360.02";

    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IChartOfAccountsRepository _chartOfAccountsRepository;
    private readonly ITaxWithholdingRepository _taxWithholdingRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TaxPrepAgent> _logger;

    public TaxPrepAgent(
        IJournalEntryRepository journalEntryRepository,
        IChartOfAccountsRepository chartOfAccountsRepository,
        ITaxWithholdingRepository taxWithholdingRepository,
        ITenantProvider tenantProvider,
        ILogger<TaxPrepAgent> logger)
    {
        _journalEntryRepository = journalEntryRepository;
        _chartOfAccountsRepository = chartOfAccountsRepository;
        _taxWithholdingRepository = taxWithholdingRepository;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<TaxPrepReport> PrepareMonthlyTaxAsync(
        Guid tenantId, int year, int month, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[TaxPrep] Aylik vergi taslagi hazirlaniyor: tenant={TenantId}, donem={Year}-{Month:D2}",
            tenantId, year, month);

        // Donem baslangic ve bitis tarihleri
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        // 1. Donem icindeki tum yevmiyeleri al (yalnizca IsPosted=true)
        var journalEntries = await _journalEntryRepository.GetByDateRangeAsync(
            tenantId, periodStart, periodEnd, ct).ConfigureAwait(false);

        var postedEntries = journalEntries.Where(je => je.IsPosted).ToList();

        _logger.LogDebug(
            "[TaxPrep] {Total} yevmiye bulundu, {Posted} tanesi islenmis",
            journalEntries.Count, postedEntries.Count);

        // 2. Hesap kodlarını cek (391, 191 hesaplarinin ID'leri)
        var account391 = await _chartOfAccountsRepository.GetByCodeAsync(tenantId, AccountCode391, ct).ConfigureAwait(false);
        var account191 = await _chartOfAccountsRepository.GetByCodeAsync(tenantId, AccountCode191, ct).ConfigureAwait(false);
        var account360_02 = await _chartOfAccountsRepository.GetByCodeAsync(tenantId, AccountCode360_02, ct).ConfigureAwait(false);

        // 3. Hesaplanan KDV (391): Satis yevmiyelerindeki 391 hesabina alacak kayitlari
        decimal calculatedVAT = 0m;
        if (account391 != null)
        {
            calculatedVAT = postedEntries
                .SelectMany(je => je.Lines)
                .Where(l => l.AccountId == account391.Id)
                .Sum(l => l.Credit);
        }

        // 4. Indirilecek KDV (191): Alis yevmiyelerindeki 191 hesabina borc kayitlari
        decimal deductibleVAT = 0m;
        if (account191 != null)
        {
            deductibleVAT = postedEntries
                .SelectMany(je => je.Lines)
                .Where(l => l.AccountId == account191.Id)
                .Sum(l => l.Debit);
        }

        // 5. Odenecek KDV = 391 - 191
        var payableVAT = calculatedVAT - deductibleVAT;

        // 6. Toplam satis ve alis (gelir ve gider hesaplarindan)
        // Satis: 600.xx hesaplarina alacak kayitlari
        var totalSales = postedEntries
            .SelectMany(je => je.Lines)
            .Where(l => l.Account != null && l.Account.Code.StartsWith("600", StringComparison.Ordinal))
            .Sum(l => l.Credit);

        // Alis: 150-159 hesaplarina borc kayitlari (stok girisleri)
        var totalPurchases = postedEntries
            .SelectMany(je => je.Lines)
            .Where(l => l.Account != null && l.Account.Code.StartsWith("15", StringComparison.Ordinal))
            .Sum(l => l.Debit);

        // 7. Tevkifat toplami (TaxWithholding kayitlarindan)
        var totalWithholding = await _taxWithholdingRepository.GetTotalWithholdingAsync(
            tenantId, periodStart, periodEnd, ct).ConfigureAwait(false);

        // 8. Stopaj toplami (360.02 hesabindaki alacak kayitlari)
        decimal totalStopaj = 0m;
        if (account360_02 != null)
        {
            totalStopaj = postedEntries
                .SelectMany(je => je.Lines)
                .Where(l => l.AccountId == account360_02.Id)
                .Sum(l => l.Credit);
        }

        // 9. Detay satirlari olustur
        var details = new List<TaxLineItem>
        {
            new("Hesaplanan KDV (satis)", AccountCode391,
                calculatedVAT),
            new("Indirilecek KDV (alis)", AccountCode191,
                deductibleVAT),
            new("Odenecek KDV (391-191)", "360.01",
                payableVAT),
            new("Tevkifat toplami", "---",
                totalWithholding),
            new("Stopaj toplami (Gelir Vergisi)", AccountCode360_02,
                totalStopaj)
        };

        // Devreden KDV varsa detaya ekle
        if (payableVAT < 0)
        {
            details.Add(new TaxLineItem(
                "Devreden KDV (sonraki aya)", "190",
                Math.Abs(payableVAT)));
        }

        var report = new TaxPrepReport(
            Year: year,
            Month: month,
            TotalSales: totalSales,
            TotalPurchases: totalPurchases,
            CalculatedVAT: calculatedVAT,
            DeductibleVAT: deductibleVAT,
            PayableVAT: payableVAT,
            TotalWithholding: totalWithholding,
            TotalStopaj: totalStopaj,
            Disclaimer: TaxDisclaimer,
            Details: details);

        _logger.LogInformation(
            "[TaxPrep] Vergi taslagi tamamlandi — Donem: {Year}-{Month:D2}, " +
            "Hesaplanan KDV: {Calc:F2}, Indirilecek KDV: {Ded:F2}, " +
            "Odenecek KDV: {Pay:F2}, Tevkifat: {With:F2}, Stopaj: {Stop:F2}",
            year, month, calculatedVAT, deductibleVAT,
            payableVAT, totalWithholding, totalStopaj);

        return report;
    }
}
