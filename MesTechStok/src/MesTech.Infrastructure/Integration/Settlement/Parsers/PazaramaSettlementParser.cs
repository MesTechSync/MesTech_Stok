using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Pazarama REST API JSON settlement parser.
/// Parses settlement data from Pazarama's /settlements endpoint.
/// Authentication: OAuth2 token.
/// Fields: orderId, amount, commission, cargoFee, netPayout.
/// </summary>
public sealed class PazaramaSettlementParser : ISettlementParser
{
    private readonly ILogger<PazaramaSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Parsed items cached between ParseAsync and ParseLinesAsync calls
    private List<PazaramaSettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => nameof(PlatformType.Pazarama);

    public PazaramaSettlementParser(ILogger<PazaramaSettlementParser> logger)
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

        _logger.LogInformation("[PazaramaSettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Deserialize JSON
        var response = await JsonSerializer.DeserializeAsync<PazaramaSettlementResponse>(
            rawData, _jsonOptions, ct).ConfigureAwait(false);

        if (response is null || response.Settlements.Count == 0)
        {
            _logger.LogWarning("[PazaramaSettlementParser] Empty or null response, creating empty batch");
            _cachedItems = new List<PazaramaSettlementItem>();

            return SettlementBatch.Create(
                tenantId: tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        _cachedItems = response.Settlements;

        // Calculate totals from all items
        var totalGross = _cachedItems.Sum(i => i.Amount);
        var totalCommission = _cachedItems.Sum(i => i.Commission);
        var totalNet = _cachedItems.Sum(i => i.NetPayout);

        // Determine period from response metadata or transaction dates
        var periodStart = ParseDate(response.PeriodStart);
        var periodEnd = ParseDate(response.PeriodEnd);

        if (periodStart is null || periodEnd is null)
        {
            var dates = _cachedItems
                .Where(i => !string.IsNullOrEmpty(i.TransactionDate))
                .Select(i => ParseDate(i.TransactionDate ?? string.Empty))
                .OfType<DateTime>()
                .ToList();

            periodStart ??= dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date;
            periodEnd ??= dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date;
        }

        var batch = SettlementBatch.Create(
            tenantId: tenantId,
            platform: Platform,
            periodStart: periodStart.Value,
            periodEnd: periodEnd.Value,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalNet);

        _logger.LogInformation(
            "[PazaramaSettlementParser] Parsed batch: {ItemCount} items, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
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
            _logger.LogWarning("[PazaramaSettlementParser] No cached items — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedItems.Count);

        foreach (var item in _cachedItems)
        {
            ct.ThrowIfCancellationRequested();

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: item.OrderId,
                grossAmount: item.Amount,
                commissionAmount: item.Commission,
                serviceFee: 0m, // Pazarama does not expose a separate service fee
                cargoDeduction: item.CargoFee,
                refundDeduction: 0m, // Pazarama refunds are separate
                netAmount: item.NetPayout);

            lines.Add(line);
            batch.AddLine(line);

            // Auto-create CommissionRecord for each line with commission
            if (item.Commission != 0m)
            {
                _ = CommissionRecord.Create(
                    tenantId: batch.TenantId,
                    platform: Platform,
                    grossAmount: item.Amount,
                    commissionRate: item.CommissionRate,
                    commissionAmount: item.Commission,
                    serviceFee: 0m,
                    orderId: item.OrderId,
                    category: item.Category);
            }
        }

        _logger.LogInformation(
            "[PazaramaSettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        // Pazarama may use dd.MM.yyyy or dd/MM/yyyy formats
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
