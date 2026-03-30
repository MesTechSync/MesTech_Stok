using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// WooCommerce settlement parser — parses WooCommerce order export data.
/// SPECIAL: Like OpenCart, no marketplace commission (own store).
/// Commission = 0, but includes payment gateway fees (Stripe/PayPal/iyzico).
/// Platform = "WooCommerce".
/// </summary>
public sealed class WooCommerceSettlementParser : ISettlementParser
{
    private readonly ILogger<WooCommerceSettlementParser> _logger;
    private List<WooCommerceOrder>? _cachedOrders;
    private Guid _tenantId;

    public string Platform => "WooCommerce";

    public WooCommerceSettlementParser(ILogger<WooCommerceSettlementParser> logger)
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
        using var sha = SHA256.Create();
        await sha.ComputeHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        using var doc = await JsonDocument.ParseAsync(rawData, cancellationToken: ct).ConfigureAwait(false);
        _cachedOrders = new List<WooCommerceOrder>();

        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
                _cachedOrders.Add(ParseOrder(el));
        }
        else if (root.TryGetProperty("orders", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
                _cachedOrders.Add(ParseOrder(el));
        }

        if (_cachedOrders.Count == 0)
            return SettlementBatch.Create(_tenantId, Platform, DateTime.UtcNow.Date, DateTime.UtcNow.Date, 0m, 0m, 0m);

        var totalGross = _cachedOrders.Sum(o => o.Total);
        var totalNet = _cachedOrders.Sum(o => o.Total - o.GatewayFee - o.ShippingTotal);

        var dates = _cachedOrders.Where(o => o.DateCreated.HasValue).Select(o => o.DateCreated!.Value).ToList();

        var batch = SettlementBatch.Create(_tenantId, Platform,
            dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date,
            dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date,
            totalGross, 0m, totalNet);

        _logger.LogInformation("[WooCommerceSettlementParser] {Count} orders, Gross={G:F2}, Net={N:F2}",
            _cachedOrders.Count, totalGross, totalNet);
        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (_cachedOrders is null || _cachedOrders.Count == 0)
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());

        var lines = new List<SettlementLine>();
        foreach (var o in _cachedOrders)
        {
            ct.ThrowIfCancellationRequested();
            var line = SettlementLine.Create(batch.TenantId, batch.Id, o.OrderId?.ToString(),
                grossAmount: o.Total,
                commissionAmount: 0m,
                serviceFee: o.GatewayFee,
                cargoDeduction: o.ShippingTotal,
                refundDeduction: o.RefundTotal,
                netAmount: o.Total - o.GatewayFee - o.ShippingTotal);
            lines.Add(line);
            batch.AddLine(line);
        }
        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static WooCommerceOrder ParseOrder(JsonElement el)
    {
        var o = new WooCommerceOrder();
        if (el.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number) o.OrderId = id.GetInt64();
        if (el.TryGetProperty("total", out var t)) o.Total = ParseDec(t);
        if (el.TryGetProperty("shipping_total", out var st)) o.ShippingTotal = ParseDec(st);
        if (el.TryGetProperty("total_refunded", out var tr)) o.RefundTotal = ParseDec(tr);
        if (el.TryGetProperty("gateway_fee", out var gf)) o.GatewayFee = ParseDec(gf);
        if (el.TryGetProperty("date_created", out var dc))
        {
            var ds = dc.GetString();
            if (!string.IsNullOrEmpty(ds) && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                o.DateCreated = d;
        }
        return o;
    }

    private static decimal ParseDec(JsonElement el) =>
        el.ValueKind == JsonValueKind.String
            ? decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m
            : el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : 0m;

    private sealed class WooCommerceOrder
    {
        public long? OrderId { get; set; }
        public decimal Total { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal RefundTotal { get; set; }
        public decimal GatewayFee { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
