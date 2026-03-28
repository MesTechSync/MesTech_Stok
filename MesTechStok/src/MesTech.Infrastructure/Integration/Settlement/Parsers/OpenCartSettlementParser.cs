using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// OpenCart order data settlement parser.
/// SPECIAL: No platform commission (own store).
/// Commission = 0, but has CargoExpense and payment gateway fee (gatewayFee → ServiceFee).
/// Parses from order export data (simulated DB query results as JSON).
/// </summary>
public sealed class OpenCartSettlementParser : ISettlementParser
{
    private readonly ILogger<OpenCartSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Parsed items cached between ParseAsync and ParseLinesAsync calls
    private List<OpenCartSettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => "OpenCart";

    public OpenCartSettlementParser(ILogger<OpenCartSettlementParser> logger)
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

        _logger.LogInformation("[OpenCartSettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Deserialize JSON
        var response = await JsonSerializer.DeserializeAsync<OpenCartSettlementResponse>(
            rawData, _jsonOptions, ct).ConfigureAwait(false);

        if (response is null || response.Orders.Count == 0)
        {
            _logger.LogWarning("[OpenCartSettlementParser] Empty or null response, creating empty batch");
            _cachedItems = new List<OpenCartSettlementItem>();

            return SettlementBatch.Create(
                tenantId: tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        _cachedItems = response.Orders;

        // Calculate totals — Commission is always 0 for own store
        var totalGross = _cachedItems.Sum(i => i.OrderTotal);
        var totalCommission = 0m; // No platform commission for OpenCart
        var totalNet = _cachedItems.Sum(i => i.NetAmount);

        // Determine period from response metadata or order dates
        var periodStart = ParseDate(response.PeriodStart);
        var periodEnd = ParseDate(response.PeriodEnd);

        if (periodStart is null || periodEnd is null)
        {
            var dates = _cachedItems
                .Where(i => !string.IsNullOrEmpty(i.OrderDate))
                .Select(i => ParseDate(i.OrderDate ?? string.Empty))
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
            "[OpenCartSettlementParser] Parsed batch: {ItemCount} orders, Gross={Gross:F2}, Commission=0.00 (own store), Net={Net:F2}, Hash={Hash}",
            _cachedItems.Count,
            totalGross.ToString("F2", CultureInfo.InvariantCulture),
            totalNet.ToString("F2", CultureInfo.InvariantCulture),
            _rawFileHash);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_cachedItems is null || _cachedItems.Count == 0)
        {
            _logger.LogWarning("[OpenCartSettlementParser] No cached items — was ParseAsync called first?");
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
                grossAmount: item.OrderTotal,
                commissionAmount: 0m, // No platform commission for own store
                serviceFee: item.GatewayFee, // Payment gateway fee (iyzico, PayTR, etc.)
                cargoDeduction: item.CargoExpense,
                refundDeduction: 0m,
                netAmount: item.NetAmount);

            lines.Add(line);
            batch.AddLine(line);

            // No CommissionRecord for OpenCart — commission is always 0
            // Gateway fee is tracked in SettlementLine.ServiceFee
        }

        _logger.LogInformation(
            "[OpenCartSettlementParser] Created {LineCount} settlement lines for batch {BatchId} (own store, zero commission)",
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

        // OpenCart may use yyyy-MM-dd HH:mm:ss or dd/MM/yyyy formats
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
