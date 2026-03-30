using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// Amazon EU settlement parser — aynı SP-API TSV formatını kullanır.
/// AmazonTR parser'ı delegate eder, sadece Platform property farklı.
/// Settlement 14/16 → 15/16 (Bitrix24 CRM — settlement uygulanamaz).
/// </summary>
public sealed class AmazonEuSettlementParser : ISettlementParser
{
    private readonly AmazonSettlementParser _inner;

    public string Platform => "AmazonEu";

    public AmazonEuSettlementParser(ILogger<AmazonSettlementParser> logger)
    {
        _inner = new AmazonSettlementParser(logger);
    }

    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
        => _inner.ParseAsync(rawData, format, ct);

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
        => _inner.ParseLinesAsync(batch, ct);
}
