using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Zalando Partner settlement parser — parses Zalando ZDirect partner finance reports.
/// Commission = Zalando commission (category-based %) + fulfillment fees.
/// Platform = "Zalando".
/// </summary>
public sealed class ZalandoSettlementParser : ISettlementParser
{
    private readonly ILogger<ZalandoSettlementParser> _logger;
    private List<ZalandoTransaction>? _cachedTransactions;
    private string? _rawFileHash;
    private Guid _tenantId;

    public string Platform => nameof(PlatformType.Zalando);

    public ZalandoSettlementParser(ILogger<ZalandoSettlementParser> logger)
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
        _cachedTransactions = new List<ZalandoTransaction>();

        var root = doc.RootElement;
        JsonElement itemsArr = default;
        if (root.TryGetProperty("items", out itemsArr) || root.TryGetProperty("transactions", out itemsArr))
        {
            if (itemsArr.ValueKind == JsonValueKind.Array)
                foreach (var el in itemsArr.EnumerateArray())
                    _cachedTransactions.Add(ParseTx(el));
        }

        if (_cachedTransactions.Count == 0)
            return SettlementBatch.Create(_tenantId, Platform, DateTime.UtcNow.Date, DateTime.UtcNow.Date, 0m, 0m, 0m);

        var totalGross = _cachedTransactions.Where(t => t.Type == "SALE").Sum(t => t.GrossAmount);
        var totalCommission = _cachedTransactions.Sum(t => t.CommissionAmount);
        var totalNet = _cachedTransactions.Sum(t => t.NetAmount);

        var dates = _cachedTransactions.Where(t => t.TransactionDate.HasValue).Select(t => t.TransactionDate!.Value).ToList();

        var batch = SettlementBatch.Create(_tenantId, Platform,
            dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date,
            dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date,
            totalGross, totalCommission, totalNet);

        _logger.LogInformation("[ZalandoSettlementParser] {Count} tx, Gross={G:F2}, Comm={C:F2}, Net={N:F2}",
            _cachedTransactions.Count, totalGross, totalCommission, totalNet);
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
            var line = SettlementLine.Create(batch.TenantId, batch.Id, tx.OrderNumber,
                grossAmount: tx.GrossAmount,
                commissionAmount: tx.CommissionAmount,
                serviceFee: tx.FulfillmentFee,
                cargoDeduction: tx.ShippingFee,
                refundDeduction: tx.Type == "RETURN" ? Math.Abs(tx.GrossAmount) : 0m,
                netAmount: tx.NetAmount);
            lines.Add(line);
            batch.AddLine(line);
        }
        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static ZalandoTransaction ParseTx(JsonElement el)
    {
        var tx = new ZalandoTransaction();
        if (el.TryGetProperty("type", out var t)) tx.Type = t.GetString() ?? "";
        if (el.TryGetProperty("order_number", out var on)) tx.OrderNumber = on.GetString();
        if (el.TryGetProperty("gross_amount", out var ga) && ga.ValueKind == JsonValueKind.Number) tx.GrossAmount = ga.GetDecimal();
        if (el.TryGetProperty("commission_amount", out var ca) && ca.ValueKind == JsonValueKind.Number) tx.CommissionAmount = ca.GetDecimal();
        if (el.TryGetProperty("fulfillment_fee", out var ff) && ff.ValueKind == JsonValueKind.Number) tx.FulfillmentFee = ff.GetDecimal();
        if (el.TryGetProperty("shipping_fee", out var sf) && sf.ValueKind == JsonValueKind.Number) tx.ShippingFee = sf.GetDecimal();
        if (el.TryGetProperty("net_amount", out var na) && na.ValueKind == JsonValueKind.Number) tx.NetAmount = na.GetDecimal();
        if (el.TryGetProperty("transaction_date", out var td))
        {
            var ds = td.GetString();
            if (!string.IsNullOrEmpty(ds) && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                tx.TransactionDate = d;
        }
        return tx;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream s, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(s, ct).ConfigureAwait(false));
    }

    private sealed class ZalandoTransaction
    {
        public string Type { get; set; } = "";
        public string? OrderNumber { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal FulfillmentFee { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
}
