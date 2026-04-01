using System.Diagnostics.CodeAnalysis;
using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;

/// <summary>
/// Accepts raw settlement file data (JSON/TSV/CSV from platform API or file upload),
/// parses via platform-specific ISettlementParser, and persists the batch.
/// Bridges the gap between ISettlementParserFactory and settlement persistence.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Binary data")]
public record ParseAndImportSettlementCommand(
    Guid TenantId,
    string Platform,
    byte[] RawData,
    string Format
) : IRequest<Guid>;
