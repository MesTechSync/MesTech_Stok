using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;

public record GetPenaltyRecordByIdQuery(Guid Id) : IRequest<PenaltyRecordDto?>;
