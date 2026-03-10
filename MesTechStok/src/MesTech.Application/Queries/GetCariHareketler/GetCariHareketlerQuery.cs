using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Queries.GetCariHareketler;

public record GetCariHareketlerQuery(
    Guid CariHesapId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<IReadOnlyList<CariHareketDto>>;
