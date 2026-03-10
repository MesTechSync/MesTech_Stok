using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.GetCariHesaplar;

public record GetCariHesaplarQuery(
    CariHesapType? Type = null,
    Guid? TenantId = null
) : IRequest<IReadOnlyList<CariHesapDto>>;
