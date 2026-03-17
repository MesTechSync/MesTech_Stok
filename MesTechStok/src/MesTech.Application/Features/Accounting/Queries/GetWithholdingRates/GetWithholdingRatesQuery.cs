using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;

/// <summary>
/// KDV tevkifat oranlari sorgusu — GiB resmi listesi.
/// </summary>
public record GetWithholdingRatesQuery : IRequest<List<WithholdingRateDto>>;
