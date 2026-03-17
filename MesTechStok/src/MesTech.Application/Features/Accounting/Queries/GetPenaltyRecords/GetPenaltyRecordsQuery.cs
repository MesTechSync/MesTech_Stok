using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;

public record GetPenaltyRecordsQuery(
    Guid TenantId,
    PenaltySource? Source = null
) : IRequest<IReadOnlyList<PenaltyRecordDto>>;
