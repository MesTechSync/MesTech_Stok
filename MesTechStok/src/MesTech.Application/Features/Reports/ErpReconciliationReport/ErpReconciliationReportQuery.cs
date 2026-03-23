using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Reports.ErpReconciliationReport;

public record ErpReconciliationReportQuery(
    Guid TenantId,
    ErpProvider ErpProvider
) : IRequest<ErpReconciliationReportDto>;

public record ErpReconciliationReportDto
{
    public ErpProvider ErpProvider { get; init; }
    public int TotalMesTechContacts { get; init; }
    public int TotalErpContacts { get; init; }
    public int MatchedCount { get; init; }
    public int UnmatchedInMesTech { get; init; }
    public int UnmatchedInErp { get; init; }
    public IReadOnlyList<UnmatchedContactDto> UnmatchedItems { get; init; } = [];
    public DateTime GeneratedAt { get; init; }
}

public record UnmatchedContactDto(
    string Source,
    string Name,
    string? TaxNumber,
    string Reason);
