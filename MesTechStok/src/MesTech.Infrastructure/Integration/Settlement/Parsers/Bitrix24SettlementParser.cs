using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Bitrix24 CRM deal settlement parser.
/// Parses deal export data (JSON) into settlement batches.
/// Commission = platform subscription fee allocation per deal.
/// </summary>
public sealed class Bitrix24SettlementParser : ISettlementParser
{
    private readonly ILogger<Bitrix24SettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<Bitrix24SettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => "Bitrix24";

    public Bitrix24SettlementParser(ILogger<Bitrix24SettlementParser> logger)
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

        _logger.LogInformation("[Bitrix24SettlementParser] Parsing settlement data (format: {Format})", format);

        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        var response = await JsonSerializer.DeserializeAsync<Bitrix24SettlementResponse>(
            rawData, _jsonOptions, ct).ConfigureAwait(false);

        if (response is null || response.Deals.Count == 0)
        {
            _logger.LogWarning("[Bitrix24SettlementParser] Empty or null response, creating empty batch");
            _cachedItems = new List<Bitrix24SettlementItem>();

            return SettlementBatch.Create(
                tenantId: tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        _cachedItems = response.Deals;

        var totalGross = _cachedItems.Sum(i => i.Opportunity);
        var totalCommission = _cachedItems.Sum(i => i.CommissionAmount);
        var totalNet = _cachedItems.Sum(i => i.NetAmount);

        var periodStart = ParseDate(response.PeriodStart);
        var periodEnd = ParseDate(response.PeriodEnd);

        if (periodStart is null || periodEnd is null)
        {
            var dates = _cachedItems
                .Where(i => !string.IsNullOrEmpty(i.CloseDate))
                .Select(i => ParseDate(i.CloseDate ?? string.Empty))
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
            "[Bitrix24SettlementParser] Parsed batch: {ItemCount} deals, Gross={Gross}, Commission={Commission}, Net={Net}, Hash={Hash}",
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
            _logger.LogWarning("[Bitrix24SettlementParser] No cached items — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedItems.Count);

        foreach (var item in _cachedItems)
        {
            ct.ThrowIfCancellationRequested();

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: item.DealId,
                grossAmount: item.Opportunity,
                commissionAmount: item.CommissionAmount,
                serviceFee: 0m,
                cargoDeduction: item.CargoAmount,
                refundDeduction: 0m,
                netAmount: item.NetAmount);

            lines.Add(line);
            batch.AddLine(line);
        }

        _logger.LogInformation(
            "[Bitrix24SettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
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

        string[] formats = { "yyyy-MM-dd HH:mm:ss", "dd.MM.yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
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
