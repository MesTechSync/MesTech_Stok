using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// eBay settlement parser — basic implementation for eBay Finances API v1.
/// Parses transaction data from /sell/finances/v1/transaction endpoint.
/// Maps eBay transaction types (SALE, REFUND, SHIPPING_LABEL, etc.) to MesTech SettlementLine.
/// Platform = "eBay".
/// </summary>
public sealed class EbaySettlementParser : ISettlementParser
{
    private readonly ILogger<EbaySettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Parsed transactions cached between ParseAsync and ParseLinesAsync calls
    private List<EbayTransaction>? _cachedTransactions;
    private string? _rawFileHash;

    // Tenant ID set from the tenantId overload — required for multi-tenant safety
    private Guid _tenantId;

    public string Platform => nameof(PlatformType.eBay);

    public EbaySettlementParser(ILogger<EbaySettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
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
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be Guid.Empty — multi-tenant safety.", nameof(tenantId));

        _tenantId = tenantId;
        _logger.LogInformation("[EbaySettlementParser] Parsing settlement data (format: {Format})", format);

        // Compute SHA256 hash of raw stream for deduplication
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        // Deserialize JSON — eBay Finances API response
        using var doc = await JsonDocument.ParseAsync(rawData, cancellationToken: ct).ConfigureAwait(false);
        _cachedTransactions = new List<EbayTransaction>();

        if (doc.RootElement.TryGetProperty("transactions", out var transactionsArr))
        {
            foreach (var txEl in transactionsArr.EnumerateArray())
            {
                _cachedTransactions.Add(ParseTransaction(txEl));
            }
        }

        if (_cachedTransactions.Count == 0)
        {
            _logger.LogWarning("[EbaySettlementParser] Empty or null response, creating empty batch");

            return SettlementBatch.Create(
                tenantId: _tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        // Calculate totals from all SALE transactions
        var totalGross = _cachedTransactions
            .Where(t => t.TransactionType == "SALE")
            .Sum(t => t.TotalFeeBasisAmount);

        var totalCommission = _cachedTransactions
            .Sum(t => t.TotalFeeAmount);

        var totalNet = _cachedTransactions
            .Sum(t => t.Amount);

        // Determine period from transaction dates
        var dates = _cachedTransactions
            .Where(t => t.TransactionDate.HasValue)
            .Select(t => t.TransactionDate!.Value)
            .ToList();

        var periodStart = dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date;
        var periodEnd = dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date;

        var batch = SettlementBatch.Create(
            tenantId: _tenantId,
            platform: Platform,
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalGross: totalGross,
            totalCommission: totalCommission,
            totalNet: totalNet);

        _logger.LogInformation(
            "[EbaySettlementParser] Parsed batch: {TxCount} transactions, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
            _cachedTransactions.Count,
            totalGross.ToString("F2", CultureInfo.InvariantCulture),
            totalCommission.ToString("F2", CultureInfo.InvariantCulture),
            totalNet.ToString("F2", CultureInfo.InvariantCulture),
            _rawFileHash);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_cachedTransactions is null || _cachedTransactions.Count == 0)
        {
            _logger.LogWarning("[EbaySettlementParser] No cached transactions — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedTransactions.Count);

        foreach (var tx in _cachedTransactions)
        {
            ct.ThrowIfCancellationRequested();

            // Map eBay transaction fields to MesTech settlement line fields:
            // GrossAmount = totalFeeBasisAmount (the sale amount before fees)
            // CommissionAmount = totalFeeAmount (eBay's total fees including final value fee)
            // ServiceFee = 0 (eBay bundles all fees into totalFeeAmount)
            // CargoDeduction = shipping label cost if transaction type is SHIPPING_LABEL
            // RefundDeduction = amount if transaction type is REFUND (negative)
            // NetAmount = amount (what the seller actually receives)

            var gross = tx.TotalFeeBasisAmount;
            var commission = tx.TotalFeeAmount;
            var serviceFee = 0m;
            var cargoDeduction = tx.TransactionType == "SHIPPING_LABEL" ? Math.Abs(tx.Amount) : 0m;
            var refundDeduction = tx.TransactionType == "REFUND" ? Math.Abs(tx.Amount) : 0m;
            var net = tx.Amount;

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: tx.OrderId,
                grossAmount: gross,
                commissionAmount: commission,
                serviceFee: serviceFee,
                cargoDeduction: cargoDeduction,
                refundDeduction: refundDeduction,
                netAmount: net);

            lines.Add(line);
            batch.AddLine(line);
        }

        _logger.LogInformation(
            "[EbaySettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    /// <summary>
    /// Parses a single eBay Finances API transaction element.
    /// eBay format: { transactionId, transactionType, amount: { value, currency },
    ///               transactionDate, orderId, totalFeeBasisAmount: { value }, totalFeeAmount: { value } }
    /// </summary>
    private static EbayTransaction ParseTransaction(JsonElement txEl)
    {
        var tx = new EbayTransaction();

        if (txEl.TryGetProperty("transactionId", out var tidEl))
            tx.TransactionId = tidEl.GetString() ?? string.Empty;

        if (txEl.TryGetProperty("transactionType", out var ttEl))
            tx.TransactionType = ttEl.GetString() ?? string.Empty;

        if (txEl.TryGetProperty("orderId", out var oidEl))
            tx.OrderId = oidEl.GetString();

        if (txEl.TryGetProperty("transactionDate", out var tdEl))
        {
            var dateStr = tdEl.GetString();
            if (!string.IsNullOrEmpty(dateStr) &&
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                tx.TransactionDate = parsedDate;
            }
        }

        // Parse amount: { "value": "25.99", "currency": "USD" }
        if (txEl.TryGetProperty("amount", out var amountEl))
        {
            tx.Amount = ParseMoneyValue(amountEl);
            if (amountEl.TryGetProperty("currency", out var currEl))
                tx.Currency = currEl.GetString() ?? "USD";
        }

        // Parse totalFeeBasisAmount: { "value": "25.99" }
        if (txEl.TryGetProperty("totalFeeBasisAmount", out var basisEl))
            tx.TotalFeeBasisAmount = ParseMoneyValue(basisEl);

        // Parse totalFeeAmount: { "value": "3.38" }
        if (txEl.TryGetProperty("totalFeeAmount", out var feeEl))
            tx.TotalFeeAmount = ParseMoneyValue(feeEl);

        return tx;
    }

    /// <summary>
    /// Parses eBay money value object: { "value": "25.99" } or { "value": "25.99", "currency": "USD" }.
    /// eBay always sends value as a string.
    /// </summary>
    private static decimal ParseMoneyValue(JsonElement moneyEl)
    {
        if (moneyEl.TryGetProperty("value", out var valEl))
        {
            var valStr = valEl.GetString();
            if (!string.IsNullOrEmpty(valStr) &&
                decimal.TryParse(valStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }

        return 0m;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Internal model for eBay Finances API transaction.
    /// Not exposed outside the parser — maps directly from JSON.
    /// </summary>
    private sealed class EbayTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal TotalFeeBasisAmount { get; set; }
        public decimal TotalFeeAmount { get; set; }
    }
}
