using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Ozon settlement parser — parses Ozon Finance API v3 transaction reports.
/// Ozon JSON format: { "result": { "operations": [ { "operation_id", "operation_type", "posting": { "posting_number" }, ... } ] } }
/// Maps Ozon operation types (OperationAgentDeliveredToCustomer, OperationReturn, etc.) to MesTech SettlementLine.
/// Platform = "Ozon".
/// </summary>
public sealed class OzonSettlementParser : ISettlementParser
{
    private readonly ILogger<OzonSettlementParser> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<OzonOperation>? _cachedOperations;
    private string? _rawFileHash;
    private Guid _tenantId;

    public string Platform => "Ozon";

    public OzonSettlementParser(ILogger<OzonSettlementParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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
        _logger.LogInformation("[OzonSettlementParser] Parsing settlement data (format: {Format})", format);

        _rawFileHash = await ComputeStreamHashAsync(rawData, ct).ConfigureAwait(false);
        rawData.Position = 0;

        using var doc = await JsonDocument.ParseAsync(rawData, cancellationToken: ct).ConfigureAwait(false);
        _cachedOperations = new List<OzonOperation>();

        // Ozon Finance API response: { "result": { "operations": [...] } }
        var root = doc.RootElement;
        JsonElement operationsArr = default;

        if (root.TryGetProperty("result", out var resultEl) &&
            resultEl.TryGetProperty("operations", out operationsArr))
        {
            // Standard Finance API response
        }
        else if (root.TryGetProperty("operations", out operationsArr))
        {
            // Direct operations array
        }

        if (operationsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var opEl in operationsArr.EnumerateArray())
            {
                _cachedOperations.Add(ParseOperation(opEl));
            }
        }

        if (_cachedOperations.Count == 0)
        {
            _logger.LogWarning("[OzonSettlementParser] Empty or null response, creating empty batch");

            return SettlementBatch.Create(
                tenantId: _tenantId,
                platform: Platform,
                periodStart: DateTime.UtcNow.Date,
                periodEnd: DateTime.UtcNow.Date,
                totalGross: 0m,
                totalCommission: 0m,
                totalNet: 0m);
        }

        var totalGross = _cachedOperations
            .Where(o => o.OperationType is "OperationAgentDeliveredToCustomer" or "OperationItemReturn")
            .Sum(o => Math.Abs(o.Amount));

        var totalCommission = _cachedOperations
            .Where(o => o.OperationType is "OperationMarketplaceServiceItemFulfillment"
                or "OperationMarketplaceServiceItemDirectFlowTrans"
                or "OperationMarketplaceServiceItemReturnFlowTrans")
            .Sum(o => Math.Abs(o.Amount));

        var totalNet = _cachedOperations.Sum(o => o.Amount);

        var dates = _cachedOperations
            .Where(o => o.OperationDate.HasValue)
            .Select(o => o.OperationDate!.Value)
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
            "[OzonSettlementParser] Parsed batch: {OpCount} operations, Gross={Gross:F2}, Commission={Commission:F2}, Net={Net:F2}, Hash={Hash}",
            _cachedOperations.Count,
            totalGross.ToString("F2", CultureInfo.InvariantCulture),
            totalCommission.ToString("F2", CultureInfo.InvariantCulture),
            totalNet.ToString("F2", CultureInfo.InvariantCulture),
            _rawFileHash);

        return batch;
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_cachedOperations is null || _cachedOperations.Count == 0)
        {
            _logger.LogWarning("[OzonSettlementParser] No cached operations — was ParseAsync called first?");
            return Task.FromResult<IReadOnlyList<SettlementLine>>(Array.Empty<SettlementLine>());
        }

        var lines = new List<SettlementLine>(_cachedOperations.Count);

        foreach (var op in _cachedOperations)
        {
            ct.ThrowIfCancellationRequested();

            var isSale = op.OperationType == "OperationAgentDeliveredToCustomer";
            var isRefund = op.OperationType == "OperationItemReturn";
            var isCommission = op.OperationType?.StartsWith("OperationMarketplaceService") == true;

            var gross = isSale ? Math.Abs(op.Amount) : 0m;
            var commission = isCommission ? Math.Abs(op.Amount) : 0m;
            var refundDeduction = isRefund ? Math.Abs(op.Amount) : 0m;
            var net = op.Amount;

            var line = SettlementLine.Create(
                tenantId: batch.TenantId,
                settlementBatchId: batch.Id,
                orderId: op.PostingNumber,
                grossAmount: gross,
                commissionAmount: commission,
                serviceFee: 0m,
                cargoDeduction: 0m,
                refundDeduction: refundDeduction,
                netAmount: net);

            lines.Add(line);
            batch.AddLine(line);
        }

        _logger.LogInformation(
            "[OzonSettlementParser] Created {LineCount} settlement lines for batch {BatchId}",
            lines.Count, batch.Id);

        return Task.FromResult<IReadOnlyList<SettlementLine>>(lines.AsReadOnly());
    }

    private static OzonOperation ParseOperation(JsonElement opEl)
    {
        var op = new OzonOperation();

        if (opEl.TryGetProperty("operation_id", out var oidEl))
            op.OperationId = oidEl.GetInt64();

        if (opEl.TryGetProperty("operation_type", out var otEl))
            op.OperationType = otEl.GetString() ?? string.Empty;

        if (opEl.TryGetProperty("operation_date", out var odEl))
        {
            var dateStr = odEl.GetString();
            if (!string.IsNullOrEmpty(dateStr) &&
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                op.OperationDate = parsedDate;
            }
        }

        if (opEl.TryGetProperty("amount", out var amtEl) &&
            amtEl.ValueKind == JsonValueKind.Number)
        {
            op.Amount = amtEl.GetDecimal();
        }

        if (opEl.TryGetProperty("posting", out var postEl) &&
            postEl.TryGetProperty("posting_number", out var pnEl))
        {
            op.PostingNumber = pnEl.GetString();
        }

        if (opEl.TryGetProperty("operation_type_name", out var otnEl))
            op.OperationTypeName = otnEl.GetString() ?? string.Empty;

        return op;
    }

    private static async Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }

    private sealed class OzonOperation
    {
        public long OperationId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string OperationTypeName { get; set; } = string.Empty;
        public string? PostingNumber { get; set; }
        public DateTime? OperationDate { get; set; }
        public decimal Amount { get; set; }
    }
}
