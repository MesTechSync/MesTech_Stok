using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.ListQuotations;

public record ListQuotationsQuery(
    QuotationStatus? Status = null
) : IRequest<IReadOnlyList<QuotationDto>>;
