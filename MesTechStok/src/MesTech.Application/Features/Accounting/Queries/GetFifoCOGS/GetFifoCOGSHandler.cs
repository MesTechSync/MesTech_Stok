using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;

/// <summary>
/// FIFO COGS sorgu handler'i.
/// Tek urun veya tum urunler icin FIFO maliyet hesaplama servisini cagirip sonuc doner.
/// </summary>
public class GetFifoCOGSHandler : IRequestHandler<GetFifoCOGSQuery, IReadOnlyList<FifoCostResultDto>>
{
    private readonly IFifoCostCalculationService _fifoService;

    public GetFifoCOGSHandler(IFifoCostCalculationService fifoService)
        => _fifoService = fifoService;

    public async Task<IReadOnlyList<FifoCostResultDto>> Handle(
        GetFifoCOGSQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProductId.HasValue)
        {
            var result = await _fifoService.CalculateCOGSAsync(
                request.TenantId, request.ProductId.Value, cancellationToken);

            return new[] { result };
        }

        return await _fifoService.CalculateAllCOGSAsync(request.TenantId, cancellationToken);
    }
}
