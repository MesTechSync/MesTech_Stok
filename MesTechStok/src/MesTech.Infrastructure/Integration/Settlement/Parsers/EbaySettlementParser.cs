using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Infrastructure.Integration.Settlement.Parsers;

/// <summary>
/// eBay settlement parser — STUB implementation.
/// Interface compliance only. Full implementation pending eBay Finances API integration.
/// Platform = "eBay".
/// </summary>
public sealed class EbaySettlementParser : ISettlementParser
{
    public string Platform => "eBay";

    public Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default)
    {
        throw new NotImplementedException("eBay settlement parser is not yet implemented");
    }

    public Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        throw new NotImplementedException("eBay settlement parser is not yet implemented");
    }
}
