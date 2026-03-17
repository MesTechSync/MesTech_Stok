using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;

public record GetSalaryRecordsQuery(
    Guid TenantId,
    int? Year = null,
    int? Month = null
) : IRequest<IReadOnlyList<SalaryRecordDto>>;
