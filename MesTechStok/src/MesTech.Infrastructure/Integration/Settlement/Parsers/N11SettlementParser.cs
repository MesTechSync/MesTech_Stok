using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// N11 SOAP-based settlement report XML parser.
/// Parses XML response from N11 settlement report service.
/// Fields: siparisNo, urunAdi, satisTutari, komisyonTutari, kargoKesinti, netTutar.
/// </summary>
public sealed class N11SettlementParser : ISettlementParser
{
    private readonly ILogger<N11SettlementParser> _logger;

    // Parsed items cached between ParseAsync and ParseLinesAsync calls
    private List<N11SettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => "N11";

    public N11SettlementParser(ILogger<N11SettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);

        _logger.LogInformation("[N11SettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Parse SOAP XML response
        var doc = await XDocument.LoadAsync(rawData, LoadOptions.None, ct).ConfigureAwait(false);
        _cachedItems = ParseXmlItems(doc);

        if (_cachedItems.Count == 0)
        {
            _logger.LogWarning("[N11SettlementParser] No settlement items found in XML response");

            return SettlementBatch.Create(
                tenantId: Guid.Empty,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        // Calculate totals from all items
        var totalGross = _cachedItems.Sum(i => i.SatisTutari);
        var totalCommission = _cachedItems.Sum(i => i.KomisyonTutari);
        var totalNet = _cachedItems.Sum(i => i.NetTutar);

        // Determine period from transaction dates
        var dates = _cachedItems
            .Where(i => !string.IsNullOrEmpty(i.IslemTarihi))
            .Select(i => ParseDate(i.IslemTarihi ?? string.Empty))
            .OfType<DateTime>()
            .ToList();

        var periodStart = dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date;
        var periodEnd = dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date;

        var batch = SettlementBatch.Create(
            tenantId: Guid.Empty, // Will be set by the caller/command handler
            platform: Platform,
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalNet);

        _logger.LogInformation(
            "[N11SettlementParser] Parsed batch: {ItemCount} items, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
            _cachedItems.Count,
            totalGross.ToString("F2", CultureInfo.InvariantCulture),
            totalCommission.ToString("F2", CultureInfo.InvariantCulture),
            totalNet.ToString("F2", CultureInfo.InvariantCulture),
            _rawFileHash);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_cachedItems is null || _cachedItems.Count == 0)
        {
            _logger.LogWarning("[N11SettlementParser] No cached items — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedItems.Count);

        foreach (var item in _cachedItems)
        {
            ct.ThrowIfCancellationRequested();

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: item.SiparisNo,
                grossAmount: item.SatisTutari,
                commissionAmount: item.KomisyonTutari,
                serviceFee: 0m, // N11 does not expose a separate service fee
                cargoDeduction: item.KargoKesinti,
                refundDeduction: 0m, // N11 refunds are separate
                netAmount: item.NetTutar);

            lines.Add(line);
            batch.AddLine(line);

            // Auto-create CommissionRecord for each line with commission
            if (item.KomisyonTutari != 0m)
            {
                _ = CommissionRecord.Create(
                    tenantId: batch.TenantId,
                    platform: Platform,
                    grossAmount: item.SatisTutari,
                    commissionRate: item.KomisyonOrani,
                    commissionAmount: item.KomisyonTutari,
                    serviceFee: 0m,
                    orderId: item.SiparisNo,
                    category: item.Kategori);
            }
        }

        _logger.LogInformation(
            "[N11SettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    /// <summary>
    /// Parses N11 SOAP XML response, extracting settlement items from the Body.
    /// Handles both SOAP envelope and plain XML formats.
    /// </summary>
    private List<N11SettlementItem> ParseXmlItems(XDocument doc)
    {
        var items = new List<N11SettlementItem>();

        // Try to find settlement items — handle both SOAP envelope and plain XML
        var settlementElements = doc.Descendants()
            .Where(e => e.Name.LocalName is "settlementItem" or "settlement" or "hesapKesimi")
            .ToList();

        if (settlementElements.Count == 0)
        {
            // Fallback: look for any element containing siparisNo child
            settlementElements = doc.Descendants()
                .Where(e => e.Elements().Any(c => c.Name.LocalName is "siparisNo" or "orderNo"))
                .ToList();
        }

        foreach (var element in settlementElements)
        {
            var item = new N11SettlementItem
            {
                SiparisNo = GetElementValue(element, "siparisNo", "orderNo"),
                UrunAdi = GetElementValue(element, "urunAdi", "productName"),
                SatisTutari = ParseDecimal(GetElementValue(element, "satisTutari", "saleAmount")),
                KomisyonTutari = ParseDecimal(GetElementValue(element, "komisyonTutari", "commissionAmount")),
                KomisyonOrani = ParseDecimal(GetElementValue(element, "komisyonOrani", "commissionRate")),
                KargoKesinti = ParseDecimal(GetElementValue(element, "kargoKesinti", "cargoDeduction")),
                NetTutar = ParseDecimal(GetElementValue(element, "netTutar", "netAmount")),
                IslemTarihi = GetElementValue(element, "islemTarihi", "transactionDate"),
                Kategori = GetElementValue(element, "kategori", "category")
            };

            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Gets element value by local name, trying primary then fallback name.
    /// </summary>
    private static string? GetElementValue(XElement parent, string primaryName, string? fallbackName = null)
    {
        var element = parent.Elements()
            .FirstOrDefault(e => e.Name.LocalName == primaryName);

        if (element is null && fallbackName is not null)
        {
            element = parent.Elements()
                .FirstOrDefault(e => e.Name.LocalName == fallbackName);
        }

        var value = element?.Value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        // Handle Turkish decimal format (comma as decimal separator)
        if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("tr-TR"), out result))
            return result;

        return 0m;
    }

    private static DateTime? ParseDate(string dateStr)
    {
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        // N11 may use dd.MM.yyyy or dd/MM/yyyy formats
        string[] formats = { "dd.MM.yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
        {
            return result;
        }

        return null;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }
}
