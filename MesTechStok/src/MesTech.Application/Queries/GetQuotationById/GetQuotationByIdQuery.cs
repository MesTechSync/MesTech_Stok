using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetQuotationById;

public record GetQuotationByIdQuery(Guid Id) : IRequest<QuotationDto?>;
