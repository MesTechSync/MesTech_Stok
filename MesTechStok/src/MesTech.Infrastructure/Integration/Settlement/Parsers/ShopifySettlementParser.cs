using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Shopify Payments settlement parser — parses Shopify Payouts API responses.
/// Shopify JSON: { "payouts": [ { "id", "amount", "status" } ], "transactions": [ ... ] }
/// Commission = Shopify Payments processing fee + transaction fee.
/// Platform = "Shopify".
/// </summary>
public sealed class ShopifySettlementParser : ISettlementParser
{
    private readonly ILogger<ShopifySettlementParser> _logger;
    private List<ShopifyTransaction>? _cachedTransactions;
    private string? _rawFileHash;
    private Guid _tenantId;

    public string Platform => nameof(PlatformType.Shopify);

    public ShopifySettlementParser(ILogger<ShopifySettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Obsolete("Use ParseAsync(tenantId, rawData, format, ct)")]
    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
        => throw new ArgumentException("TenantId required.", nameof(rawData));

    public async Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be Guid.Empty.", nameof(tenantId));

        _tenantId = tenantId;
        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        using var doc = await JsonDocument.ParseAsync(rawData, cancellationToken: ct).ConfigureAwait(false);
        _cachedTransactions = new List<ShopifyTransaction>();

        var root = doc.RootElement;
        if (root.TryGetProperty("transactions", out var txArr) && txArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in txArr.EnumerateArray())
                _cachedTransactions.Add(ParseTx(el));
        }

        if (_cachedTransactions.Count == 0)
            return SettlementBatch.Create(_tenantId, Platform, DateTime.UtcNow.Date, DateTime.UtcNow.Date, 0m, 0m, 0m);

        var totalGross = _cachedTransactions.Where(t => t.Type == "charge").Sum(t => t.Amount);
        var totalFee = _cachedTransactions.Where(t => t.Type == "charge").Sum(t => t.Fee);
        var totalNet = _cachedTransactions.Sum(t => t.Net);

        var dates = _cachedTransactions.Where(t => t.ProcessedAt.HasValue).Select(t => t.ProcessedAt!.Value).ToList();

        var batch = SettlementBatch.Create(_tenantId, Platform,
            dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date,
            dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date,
            totalGross, totalFee, totalNet);

        _logger.LogInformation("[ShopifySettlementParser] {Count} tx, Gross={G:F2}, Fee={F:F2}, Net={N:F2}",
            _cachedTransactions.Count, totalGross, totalFee, totalNet);
        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (_cachedTransactions is null || _cachedTransactions.Count == 0)
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());

        var lines = new List<SettlementLine>();
        foreach (var tx in _cachedTransactions)
        {
            ct.ThrowIfCancellationRequested();
            var isRefund = tx.Type == "refund";
            var line = SettlementLine.Create(batch.TenantId, batch.Id, tx.OrderId,
                grossAmount: tx.Type == "charge" ? tx.Amount : 0m,
                commissionAmount: tx.Fee,
                serviceFee: 0m,
                cargoDeduction: 0m,
                refundDeduction: isRefund ? Math.Abs(tx.Amount) : 0m,
                netAmount: tx.Net);
            lines.Add(line);
            batch.AddLine(line);
        }
        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static ShopifyTransaction ParseTx(JsonElement el)
    {
        var tx = new ShopifyTransaction();
        if (el.TryGetProperty("type", out var t)) tx.Type = t.GetString() ?? "";
        if (el.TryGetProperty("source_order_id", out var oid)) tx.OrderId = oid.GetString();
        if (el.TryGetProperty("amount", out var a)) tx.Amount = ParseDecimal(a);
        if (el.TryGetProperty("fee", out var f)) tx.Fee = ParseDecimal(f);
        if (el.TryGetProperty("net", out var n)) tx.Net = ParseDecimal(n);
        if (el.TryGetProperty("processed_at", out var pa))
        {
            var ds = pa.GetString();
            if (!string.IsNullOrEmpty(ds) && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                tx.ProcessedAt = d;
        }
        return tx;
    }

    private static decimal ParseDecimal(JsonElement el) =>
        el.ValueKind == JsonValueKind.String
            ? decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m
            : el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : 0m;

    private static async Task<string> ComputeStreamHashAsync(Stream s, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(s, ct).ConfigureAwait(false));
    }

    private sealed class ShopifyTransaction
    {
        public string Type { get; set; } = "";
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal Net { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
