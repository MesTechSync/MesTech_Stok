using System.Globalization;
using System.Security.Cryptography;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Infrastructure.Integration.Settlement.Mapping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Amazon SP-API GET_V2_SETTLEMENT_REPORT_DATA_FLAT_FILE parser.
/// Tab-separated values with multi-currency support (TRY, USD, EUR).
/// Groups by settlement-id for SettlementBatch creation.
/// Maps amount-type (ItemPrice, Commission, FBA fees, etc.) to settlement line fields.
/// </summary>
public sealed class AmazonSettlementParser : ISettlementParser
{
    private readonly ILogger<AmazonSettlementParser> _logger;

    // Parsed lines cached between ParseAsync and ParseLinesAsync calls
    private List<AmazonSettlementLine>? _cachedLines;
    private string? _rawFileHash;

    public string Platform => "Amazon";

    public AmazonSettlementParser(ILogger<AmazonSettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);

        _logger.LogInformation("[AmazonSettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct);
        rawData.Position = 0;

        // Parse TSV lines
        _cachedLines = await ParseTsvAsync(rawData, ct);

        if (_cachedLines.Count == 0)
        {
            _logger.LogWarning("[AmazonSettlementParser] No data lines found in TSV");

            return SettlementBatch.Create(
                tenantId: Guid.Empty,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        // Group by order-id for aggregation
        var orderGroups = _cachedLines
            .Where(l => !string.IsNullOrEmpty(l.OrderId))
            .GroupBy(l => l.OrderId ?? string.Empty)
            .ToList();

        // Calculate totals by amount-type
        var totalGross = _cachedLines
            .Where(l => IsItemPriceType(l.AmountType))
            .Sum(l => l.Amount);

        var totalCommission = Math.Abs(_cachedLines
            .Where(l => IsCommissionType(l.AmountType))
            .Sum(l => l.Amount));

        var totalNet = _cachedLines.Sum(l => l.Amount);

        // Determine period from settlement dates
        var periodStart = ParseSettlementDate(_cachedLines.FirstOrDefault()?.SettlementStartDate)
                          ?? DateTime.UtcNow.Date;
        var periodEnd = ParseSettlementDate(_cachedLines.FirstOrDefault()?.SettlementEndDate)
                        ?? DateTime.UtcNow.Date;

        var batch = SettlementBatch.Create(
            tenantId: Guid.Empty,
            platform: Platform,
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalNet);

        // Detect currency mix
        var currencies = _cachedLines.Select(l => l.Currency).Distinct().ToList();
        if (currencies.Count > 1)
        {
            _logger.LogWarning(
                "[AmazonSettlementParser] Multi-currency detected: {Currencies}",
                string.Join(", ", currencies));
        }

        _logger.LogInformation(
            "[AmazonSettlementParser] Parsed batch: {LineCount} TSV lines, {OrderCount} orders, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
            _cachedLines.Count,
            orderGroups.Count,
            totalGross.ToString("F2", CultureInfo.InvariantCulture),
            totalCommission.ToString("F2", CultureInfo.InvariantCulture),
            totalNet.ToString("F2", CultureInfo.InvariantCulture),
            _rawFileHash);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_cachedLines is null || _cachedLines.Count == 0)
        {
            _logger.LogWarning("[AmazonSettlementParser] No cached lines — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        // Group by order-id to create one SettlementLine per order
        var orderGroups = _cachedLines
            .Where(l => !string.IsNullOrEmpty(l.OrderId))
            .GroupBy(l => l.OrderId ?? string.Empty)
            .ToList();

        var lines = new List<SettlementLine>(orderGroups.Count);

        foreach (var group in orderGroups)
        {
            ct.ThrowIfCancellationRequested();

            var orderId = group.Key;
            var orderLines = group.ToList();

            var gross = orderLines.Where(l => IsItemPriceType(l.AmountType)).Sum(l => l.Amount);
            var commission = Math.Abs(orderLines.Where(l => IsCommissionType(l.AmountType)).Sum(l => l.Amount));
            var serviceFee = Math.Abs(orderLines.Where(l => IsFbaFeeType(l.AmountType)).Sum(l => l.Amount));
            var refundDeduction = Math.Abs(orderLines.Where(l => IsRefundType(l.TransactionType)).Sum(l => l.Amount));
            var net = orderLines.Sum(l => l.Amount);

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: orderId,
                grossAmount: gross,
                commissionAmount: commission,
                serviceFee: serviceFee,
                cargoDeduction: 0m, // Amazon includes shipping in ItemPrice
                refundDeduction: refundDeduction,
                netAmount: net);

            lines.Add(line);
            batch.AddLine(line);

            // Auto-create CommissionRecord
            if (commission != 0m)
            {
                var commissionRate = gross != 0m
                    ? Math.Round(commission / gross * 100m, 2)
                    : 0m;

                _ = CommissionRecord.Create(
                    tenantId: batch.TenantId,
                    platform: Platform,
                    grossAmount: gross,
                    commissionRate: commissionRate,
                    commissionAmount: commission,
                    serviceFee: serviceFee,
                    orderId: orderId);
            }
        }

        _logger.LogInformation(
            "[AmazonSettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    /// <summary>
    /// Parses the tab-separated flat file from Amazon SP-API.
    /// </summary>
    private static async Task<List<AmazonSettlementLine>> ParseTsvAsync(Stream stream, CancellationToken ct)
    {
        var results = new List<AmazonSettlementLine>();

        using var reader = new StreamReader(stream, leaveOpen: true);

        // Read header line
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrEmpty(headerLine))
            return results;

        var headers = headerLine.Split('\t');
        var headerIndex = BuildHeaderIndex(headers);

        // Read data lines
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split('\t');

            var item = new AmazonSettlementLine
            {
                SettlementId = GetField(fields, headerIndex, "settlement-id"),
                SettlementStartDate = GetFieldOrNull(fields, headerIndex, "settlement-start-date"),
                SettlementEndDate = GetFieldOrNull(fields, headerIndex, "settlement-end-date"),
                OrderId = GetFieldOrNull(fields, headerIndex, "order-id"),
                MarketplaceName = GetFieldOrNull(fields, headerIndex, "marketplace-name"),
                AmountType = GetFieldOrNull(fields, headerIndex, "amount-type"),
                AmountDescription = GetFieldOrNull(fields, headerIndex, "amount-description"),
                Amount = ParseDecimal(GetFieldOrNull(fields, headerIndex, "amount")),
                Currency = GetField(fields, headerIndex, "currency", "TRY"),
                TransactionType = GetFieldOrNull(fields, headerIndex, "transaction-type"),
                PostedDate = GetFieldOrNull(fields, headerIndex, "posted-date")
            };

            results.Add(item);
        }

        return results;
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            if (!string.IsNullOrEmpty(header))
                index[header] = i;
        }
        return index;
    }

    private static string GetField(string[] fields, Dictionary<string, int> headerIndex,
        string columnName, string defaultValue = "")
    {
        if (headerIndex.TryGetValue(columnName, out var idx) && idx < fields.Length)
        {
            var value = fields[idx].Trim();
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        return defaultValue;
    }

    private static string? GetFieldOrNull(string[] fields, Dictionary<string, int> headerIndex,
        string columnName)
    {
        if (headerIndex.TryGetValue(columnName, out var idx) && idx < fields.Length)
        {
            var value = fields[idx].Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0m;
    }

    private static DateTime? ParseSettlementDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            return result;

        return null;
    }

    /// <summary>ItemPrice, ProductCharges, Shipping — positive revenue items.</summary>
    private static bool IsItemPriceType(string? amountType)
        => amountType is "ItemPrice" or "ProductCharges" or "Shipping"
            or "ItemFees" or "PromotionalRebates";

    /// <summary>Commission — platform fee deduction.</summary>
    private static bool IsCommissionType(string? amountType)
        => amountType is "Commission" or "ItemFees";

    /// <summary>FBA fees — fulfillment service charges.</summary>
    private static bool IsFbaFeeType(string? amountType)
        => amountType is "FBAFees" or "FBAPerUnitFulfillmentFee"
            or "FBAPerOrderFulfillmentFee" or "FBAWeightBasedFee"
            or "ShippingHB";

    /// <summary>Refund transaction type.</summary>
    private static bool IsRefundType(string? transactionType)
        => transactionType is "Refund" or "RefundRetroCharge";

    private static async Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes);
    }
}
