using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;

public record GetTaxRecordByIdQuery(Guid Id) : IRequest<TaxRecordDto?>;
