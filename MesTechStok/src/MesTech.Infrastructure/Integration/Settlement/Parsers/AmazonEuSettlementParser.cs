using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Amazon EU settlement parser — aynı SP-API TSV formatını kullanır.
/// AmazonTR parser'ı delegate eder, sadece Platform property farklı.
/// Settlement 15/15 (Bitrix24 CRM — settlement uygulanamaz).
/// </summary>
public sealed class AmazonEuSettlementParser : ISettlementParser
{
    private readonly AmazonSettlementParser _inner;

    public string Platform => nameof(PlatformType.AmazonEu);

    public AmazonEuSettlementParser(ILogger<AmazonSettlementParser> logger)
    {
        _inner = new AmazonSettlementParser(logger, platformOverride: nameof(PlatformType.AmazonEu));
    }

    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
        => _inner.ParseAsync(rawData, format, ct);

    public Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
        => _inner.ParseAsync(tenantId, rawData, format, ct);

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
        => _inner.ParseLinesAsync(batch, ct);
}
