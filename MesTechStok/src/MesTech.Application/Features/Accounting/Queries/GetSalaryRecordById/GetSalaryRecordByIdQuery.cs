using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;

public record GetSalaryRecordByIdQuery(Guid Id) : IRequest<SalaryRecordDto?>;
