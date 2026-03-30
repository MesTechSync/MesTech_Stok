using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Etsy settlement parser — parses Etsy Payments API /v3/application/shops/{shop_id}/payment-account/ledger-entries.
/// Commission = Etsy transaction fee (6.5%) + payment processing fee (3-4%) + listing fee ($0.20).
/// Platform = "Etsy".
/// </summary>
public sealed class EtsySettlementParser : ISettlementParser
{
    private readonly ILogger<EtsySettlementParser> _logger;
    private List<EtsyLedgerEntry>? _cachedEntries;
    private string? _rawFileHash;
    private Guid _tenantId;

    public string Platform => "Etsy";

    public EtsySettlementParser(ILogger<EtsySettlementParser> logger)
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
        _cachedEntries = new List<EtsyLedgerEntry>();

        var root = doc.RootElement;
        JsonElement resultsArr = default;
        if (root.TryGetProperty("results", out resultsArr) && resultsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in resultsArr.EnumerateArray())
                _cachedEntries.Add(ParseEntry(el));
        }

        if (_cachedEntries.Count == 0)
            return SettlementBatch.Create(_tenantId, Platform, DateTime.UtcNow.Date, DateTime.UtcNow.Date, 0m, 0m, 0m);

        // Etsy amounts are in cents — convert to currency units
        var totalGross = _cachedEntries.Where(e => e.EntryType == "sale").Sum(e => e.Amount / 100m);
        var totalFee = _cachedEntries.Where(e => e.EntryType == "fee").Sum(e => Math.Abs(e.Amount) / 100m);
        var totalNet = _cachedEntries.Sum(e => e.Amount / 100m);

        var dates = _cachedEntries.Where(e => e.CreateDate.HasValue).Select(e => e.CreateDate!.Value).ToList();

        var batch = SettlementBatch.Create(_tenantId, Platform,
            dates.Count > 0 ? dates.Min() : DateTime.UtcNow.Date,
            dates.Count > 0 ? dates.Max() : DateTime.UtcNow.Date,
            totalGross, totalFee, totalNet);

        _logger.LogInformation("[EtsySettlementParser] {Count} entries, Gross={G:F2}, Fee={F:F2}, Net={N:F2}",
            _cachedEntries.Count, totalGross, totalFee, totalNet);
        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (_cachedEntries is null || _cachedEntries.Count == 0)
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());

        var lines = new List<SettlementLine>();
        foreach (var e in _cachedEntries)
        {
            ct.ThrowIfCancellationRequested();
            var isFee = e.EntryType == "fee";
            var isRefund = e.EntryType == "refund";
            var line = SettlementLine.Create(batch.TenantId, batch.Id, e.ReferenceId?.ToString(),
                grossAmount: e.EntryType == "sale" ? e.Amount / 100m : 0m,
                commissionAmount: isFee ? Math.Abs(e.Amount) / 100m : 0m,
                serviceFee: 0m, cargoDeduction: 0m,
                refundDeduction: isRefund ? Math.Abs(e.Amount) / 100m : 0m,
                netAmount: e.Amount / 100m);
            lines.Add(line);
            batch.AddLine(line);
        }
        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static EtsyLedgerEntry ParseEntry(JsonElement el)
    {
        var e = new EtsyLedgerEntry();
        if (el.TryGetProperty("entry_type", out var et)) e.EntryType = et.GetString() ?? "";
        if (el.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number) e.Amount = a.GetDecimal();
        if (el.TryGetProperty("reference_id", out var ri) && ri.ValueKind == JsonValueKind.Number) e.ReferenceId = ri.GetInt64();
        if (el.TryGetProperty("create_date", out var cd) && cd.ValueKind == JsonValueKind.Number)
            e.CreateDate = DateTimeOffset.FromUnixTimeSeconds(cd.GetInt64()).UtcDateTime;
        return e;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream s, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(s, ct).ConfigureAwait(false));
    }

    private sealed class EtsyLedgerEntry
    {
        public string EntryType { get; set; } = "";
        public decimal Amount { get; set; }
        public long? ReferenceId { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
