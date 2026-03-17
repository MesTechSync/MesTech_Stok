using System.Globalization;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;

/// <summary>
/// KDV tevkifat oranlari handler.
/// WithholdingRates static class'indan tum GiB tanimli oranlari dondurur.
/// </summary>
public class GetWithholdingRatesHandler
    : IRequestHandler<GetWithholdingRatesQuery, List<WithholdingRateDto>>
{
    public Task<List<WithholdingRateDto>> Handle(
        GetWithholdingRatesQuery request,
        CancellationToken cancellationToken)
    {
        var rates = WithholdingRates.GetAll()
            .Select(r => new WithholdingRateDto
            {
                Code = r.Code,
                Description = r.Description,
                Rate = r.Rate,
                DisplayPercent = $"%{(r.Rate * 100).ToString("N0", CultureInfo.InvariantCulture)}"
            })
            .ToList();

        return Task.FromResult(rates);
    }
}
