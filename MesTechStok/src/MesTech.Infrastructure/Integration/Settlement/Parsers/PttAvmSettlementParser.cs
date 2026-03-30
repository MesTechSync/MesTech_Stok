using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// PttAVM settlement parser — parses PttAVM seller payment reports.
/// PttAVM JSON format: { "payments": [ { "orderId", "productAmount", "commissionAmount", "cargoAmount", "netAmount", ... } ] }
/// Commission = PttAVM marketplace commission (variable %).
/// Platform = "PttAVM".
/// </summary>
public sealed class PttAvmSettlementParser : ISettlementParser
{
    private readonly ILogger<PttAvmSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<PttAvmPayment>? _cachedPayments;
    private string? _rawFileHash;
    private Guid _tenantId;

    public string Platform => "PttAVM";

    public PttAvmSettlementParser(ILogger<PttAvmSettlementParser> logger)
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
        throw new ArgumentException("TenantId is required. Use the overload with tenantId parameter.", nameof(rawData));
    }

    public async Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawData);
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be Guid.Empty — multi-tenant safety.", nameof(tenantId));

        _tenantId = tenantId;
        _logger.LogInformation("[PttAvmSettlementParser] Parsing settlement data (format: {Format})", format);

        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        using var doc = await JsonDocument.ParseAsync(rawData, cancellationToken: ct).ConfigureAwait(false);
        _cachedPayments = new List<PttAvmPayment>();

        var root = doc.RootElement;
        JsonElement paymentsArr = default;

        if (root.TryGetProperty("payments", out paymentsArr) ||
            root.TryGetProperty("data", out paymentsArr))
        {
            if (paymentsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in paymentsArr.EnumerateArray())
                    _cachedPayments.Add(ParsePayment(el));
            }
        }

        if (_cachedPayments.Count == 0)
        {
            _logger.LogWarning("[PttAvmSettlementParser] Empty response, creating empty batch");
            return SettlementBatch.Create(_tenantId, Platform, DateTime.UtcNow.Date, DateTime.UtcNow.Date, 0m, 0m, 0m);
        }

        var totalGross = _cachedPayments.Sum(p => p.ProductAmount);
        var totalCommission = _cachedPayments.Sum(p => p.CommissionAmount);
        var totalNet = _cachedPayments.Sum(p => p.NetAmount);

        var dates = _cachedPayments.Where(p => p.PaymentDate.HasValue).Select(p => p.PaymentDate!.Value).ToList();
        var periodStart = dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date;
        var periodEnd = dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date;

        var batch = SettlementBatch.Create(_tenantId, Platform, periodStart, periodEnd, totalGross, totalCommission, totalNet);

        _logger.LogInformation(
            "[PttAvmSettlementParser] Parsed {Count} payments, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}",
            _cachedPayments.Count, totalGross, totalCommission, totalNet);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (_cachedPayments is null || _cachedPayments.Count == 0)
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());

        var lines = new List<SettlementLine>(_cachedPayments.Count);
        foreach (var p in _cachedPayments)
        {
            ct.ThrowIfCancellationRequested();
            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: p.OrderId,
                grossAmount: p.ProductAmount,
                commissionAmount: p.CommissionAmount,
                serviceFee: 0m,
                cargoDeduction: p.CargoAmount,
                refundDeduction: 0m,
                netAmount: p.NetAmount);
            lines.Add(line);
            batch.AddLine(line);
        }
        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static PttAvmPayment ParsePayment(JsonElement el)
    {
        var p = new PttAvmPayment();
        if (el.TryGetProperty("orderId", out var oid)) p.OrderId = oid.GetString();
        if (el.TryGetProperty("productAmount", out var pa)) p.ProductAmount = pa.GetDecimal();
        if (el.TryGetProperty("commissionAmount", out var ca)) p.CommissionAmount = ca.GetDecimal();
        if (el.TryGetProperty("cargoAmount", out var cargo)) p.CargoAmount = cargo.GetDecimal();
        if (el.TryGetProperty("netAmount", out var na)) p.NetAmount = na.GetDecimal();
        if (el.TryGetProperty("paymentDate", out var pd))
        {
            var ds = pd.GetString();
            if (!string.IsNullOrEmpty(ds) && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                p.PaymentDate = d;
        }
        return p;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream s, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(s, ct).ConfigureAwait(false));
    }

    private sealed class PttAvmPayment
    {
        public string? OrderId { get; set; }
        public decimal ProductAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal CargoAmount { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
