using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Queries.GetKarZarar;

public record GetKarZararQuery(
    DateTime From,
    DateTime To,
    Guid? TenantId = null
) : IRequest<KarZararDto>;
