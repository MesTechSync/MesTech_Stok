using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecords;

public record GetTaxRecordsQuery(
    Guid TenantId,
    string? TaxType = null,
    int? Year = null
) : IRequest<IReadOnlyList<TaxRecordDto>>;
