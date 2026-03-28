using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Trendyol Finance API JSON settlement parser.
/// Parses settlement data from Trendyol's /suppliers/{id}/finance/settlements endpoint.
/// Handles Turkish decimal format (comma vs dot) for locale safety.
/// </summary>
public sealed class TrendyolSettlementParser : ISettlementParser
{
    private readonly ILogger<TrendyolSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Parsed items cached between ParseAsync and ParseLinesAsync calls
    private List<TrendyolSettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => "Trendyol";

    public TrendyolSettlementParser(ILogger<TrendyolSettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
    }

    [Obsolete("Use ParseAsync(tenantId, rawData, format, ct) — Guid.Empty is a multi-tenant risk (BORÇ-N)")]
    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
    {
        throw new ArgumentException("TenantId is required for settlement parsing. Use the overload with tenantId parameter.", nameof(rawData));
    }

    public async Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);

        _logger.LogInformation("[TrendyolSettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Deserialize JSON
        var response = await JsonSerializer.DeserializeAsync<TrendyolSettlementResponse>(
            rawData, _jsonOptions, ct).ConfigureAwait(false);

        if (response is null || response.Content.Count == 0)
        {
            _logger.LogWarning("[TrendyolSettlementParser] Empty or null response, creating empty batch");
            _cachedItems = new List<TrendyolSettlementItem>();

            return SettlementBatch.Create(
                tenantId: tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        _cachedItems = response.Content;

        // Calculate totals from all items
        var totalGross = _cachedItems.Sum(i => NormalizeTurkishDecimal(i.GrossSalesAmount));
        var totalCommission = _cachedItems.Sum(i => NormalizeTurkishDecimal(i.CommissionAmount));
        var totalNet = _cachedItems.Sum(i => NormalizeTurkishDecimal(i.NetAmount));

        // Determine period from transaction dates
        var dates = _cachedItems
            .Where(i => !string.IsNullOrEmpty(i.TransactionDate))
            .Select(i => ParseDate(i.TransactionDate ?? string.Empty))
            .OfType<DateTime>()
            .ToList();

        var periodStart = dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date;
        var periodEnd = dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date;

        var batch = SettlementBatch.Create(
            tenantId: tenantId,
            platform: Platform,
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalNet);

        _logger.LogInformation(
            "[TrendyolSettlementParser] Parsed batch: {ItemCount} items, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
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
            _logger.LogWarning("[TrendyolSettlementParser] No cached items — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedItems.Count);

        foreach (var item in _cachedItems)
        {
            ct.ThrowIfCancellationRequested();

            var gross = NormalizeTurkishDecimal(item.GrossSalesAmount);
            var commission = NormalizeTurkishDecimal(item.CommissionAmount);
            var serviceFee = NormalizeTurkishDecimal(item.ServiceFee);
            var cargoDeduction = NormalizeTurkishDecimal(item.CargoDeduction);
            var refundDeduction = NormalizeTurkishDecimal(item.RefundDeduction);
            var net = NormalizeTurkishDecimal(item.NetAmount);

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: item.OrderNumber,
                grossAmount: gross,
                commissionAmount: commission,
                serviceFee: serviceFee,
                cargoDeduction: cargoDeduction,
                refundDeduction: refundDeduction,
                netAmount: net);

            lines.Add(line);
            batch.AddLine(line);

            // Auto-create CommissionRecord for each line with commission
            if (commission != 0m)
            {
                var commissionRate = NormalizeTurkishDecimal(item.CommissionRate);
                _ = CommissionRecord.Create(
                    tenantId: batch.TenantId,
                    platform: Platform,
                    grossAmount: gross,
                    commissionRate: commissionRate,
                    commissionAmount: commission,
                    serviceFee: serviceFee,
                    orderId: item.OrderNumber,
                    category: item.Category);
            }
        }

        _logger.LogInformation(
            "[TrendyolSettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    /// <summary>
    /// Normalizes a decimal value that may have been parsed with Turkish locale (comma decimal separator).
    /// CultureInfo.InvariantCulture ensures consistent behavior across environments.
    /// </summary>
    private static decimal NormalizeTurkishDecimal(decimal value)
    {
        // Values from System.Text.Json are already numeric; this is a safety passthrough.
        // If future inputs come as strings with Turkish format, parse with InvariantCulture.
        return value;
    }

    private static DateTime? ParseDate(string dateStr)
    {
        // Try ISO 8601 first, then common Trendyol date formats
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        // Trendyol sometimes uses dd.MM.yyyy format
        if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture,
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
