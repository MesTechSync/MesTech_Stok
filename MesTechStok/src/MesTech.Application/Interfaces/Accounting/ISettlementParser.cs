using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Platform-specific settlement data parser — raw API/file data to domain entities.
/// Each platform (Trendyol, Amazon, Hepsiburada) provides its own parser implementation.
/// </summary>
public interface ISettlementParser
{
    /// <summary>
    /// Platform code this parser handles (e.g. "Trendyol", "Amazon", "Hepsiburada").
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Parses raw settlement data into a <see cref="SettlementBatch"/> aggregate.
    /// DEV3: tenantId overload'u kullanın — eski signature Guid.Empty ile yönlendirir.
    /// </summary>
    /// <param name="rawData">Raw stream (JSON, TSV, etc.) from platform API or file upload.</param>
    /// <param name="format">Data format hint (e.g. "json", "tsv").</param>
    /// <param name="ct">Cancellation token.</param>
    [System.Obsolete("Use ParseAsync(Guid tenantId, Stream, string, CancellationToken) — Guid.Empty is a multi-tenant risk.")]
    Task<SettlementBatch> ParseAsync(Stream rawData, string format, CancellationToken ct = default);

    /// <summary>
    /// Multi-tenant overload — parser tenantId'yi batch ve line entity'lerine atar.
    /// Yeni caller'lar bu overload'u kullanmalıdır.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation.</param>
    /// <param name="rawData">Raw stream (JSON, TSV, etc.) from platform API or file upload.</param>
    /// <param name="format">Data format hint (e.g. "json", "tsv").</param>
    /// <param name="ct">Cancellation token.</param>
    async Task<SettlementBatch> ParseAsync(Guid tenantId, Stream rawData, string format, CancellationToken ct = default)
    {
        // Default implementation: delegate to legacy overload, set tenantId on result.
        // DEV3 parsers can override this with native tenantId support.
#pragma warning disable CS0618 // Intentional call to legacy overload for backward compatibility
        var batch = await ParseAsync(rawData, format, ct).ConfigureAwait(false);
#pragma warning restore CS0618
        batch.TenantId = tenantId;
        return batch;
    }

    /// <summary>
    /// Parses individual settlement lines from a batch — called after <see cref="ParseAsync"/>.
    /// </summary>
    /// <param name="batch">The parent batch to associate lines with.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SettlementLine>> ParseLinesAsync(SettlementBatch batch, CancellationToken ct = default);
}

/// <summary>
/// Factory for resolving platform-specific settlement parsers by name.
/// </summary>
public interface ISettlementParserFactory
{
    /// <summary>
    /// Gets the parser for the given platform (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if platform is not supported.</exception>
    ISettlementParser GetParser(string platform);

    /// <summary>
    /// List of supported platform names.
    /// </summary>
    IReadOnlyList<string> SupportedPlatforms { get; }
}
