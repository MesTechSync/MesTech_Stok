using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Ciceksepeti REST API JSON settlement parser.
/// Parses settlement data from Ciceksepeti's /api/v1/settlements endpoint.
/// Authentication: x-api-key header.
/// Settlement periods: 2-week cycles.
/// Fields: orderNo, productName, saleAmount, commissionAmount, cargoContribution, serviceFee, netAmount.
/// </summary>
public sealed class CiceksepetiSettlementParser : ISettlementParser
{
    private readonly ILogger<CiceksepetiSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Parsed items cached between ParseAsync and ParseLinesAsync calls
    private List<CiceksepetiSettlementItem>? _cachedItems;
    private string? _rawFileHash;

    public string Platform => "Ciceksepeti";

    public CiceksepetiSettlementParser(ILogger<CiceksepetiSettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
    }

    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
        => ParseAsync(Guid.Empty, rawData, format, ct);

    public async Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);

        _logger.LogInformation("[CiceksepetiSettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Deserialize JSON
        var response = await JsonSerializer.DeserializeAsync<CiceksepetiSettlementResponse>(
            rawData, _jsonOptions, ct).ConfigureAwait(false);

        if (response is null || response.Items.Count == 0)
        {
            _logger.LogWarning("[CiceksepetiSettlementParser] Empty or null response, creating empty batch");
            _cachedItems = new List<CiceksepetiSettlementItem>();

            return SettlementBatch.Create(
                tenantId: tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        _cachedItems = response.Items;

        // Calculate totals from all items
        var totalGross = _cachedItems.Sum(i => i.SaleAmount);
        var totalCommission = _cachedItems.Sum(i => i.CommissionAmount);
        var totalNet = _cachedItems.Sum(i => i.NetAmount);

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
            "[CiceksepetiSettlementParser] Parsed batch: {ItemCount} items, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
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
            _logger.LogWarning("[CiceksepetiSettlementParser] No cached items — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedItems.Count);

        foreach (var item in _cachedItems)
        {
            ct.ThrowIfCancellationRequested();

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: item.OrderNo,
                grossAmount: item.SaleAmount,
                commissionAmount: item.CommissionAmount,
                serviceFee: item.ServiceFee,
                cargoDeduction: item.CargoContribution,
                refundDeduction: 0m, // Ciceksepeti refunds are separate
                netAmount: item.NetAmount);

            lines.Add(line);
            batch.AddLine(line);

            // Auto-create CommissionRecord for each line with commission
            if (item.CommissionAmount != 0m)
            {
                _ = CommissionRecord.Create(
                    tenantId: batch.TenantId,
                    platform: Platform,
                    grossAmount: item.SaleAmount,
                    commissionRate: item.CommissionRate,
                    commissionAmount: item.CommissionAmount,
                    serviceFee: item.ServiceFee,
                    orderId: item.OrderNo,
                    category: item.Category);
            }
        }

        _logger.LogInformation(
            "[CiceksepetiSettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
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

        // Ciceksepeti may use dd.MM.yyyy or dd/MM/yyyy formats
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
