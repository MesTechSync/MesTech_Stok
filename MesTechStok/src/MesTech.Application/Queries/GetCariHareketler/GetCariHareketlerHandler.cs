using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetCariHareketler;

public sealed class GetCariHareketlerHandler : IRequestHandler<GetCariHareketlerQuery, IReadOnlyList<CariHareketDto>>
{
    private readonly ICariHareketRepository _cariHareketRepository;

    public GetCariHareketlerHandler(ICariHareketRepository cariHareketRepository)
    {
        _cariHareketRepository = cariHareketRepository;
    }

    public async Task<IReadOnlyList<CariHareketDto>> Handle(GetCariHareketlerQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.From.HasValue && request.To.HasValue)
        {
            var byRange = await _cariHareketRepository.GetByDateRangeAsync(request.CariHesapId, request.From.Value, request.To.Value, cancellationToken).ConfigureAwait(false);
            return byRange.Adapt<List<CariHareketDto>>().AsReadOnly();
        }

        var all = await _cariHareketRepository.GetByCariHesapIdAsync(request.CariHesapId, cancellationToken).ConfigureAwait(false);
        return all.Adapt<List<CariHareketDto>>().AsReadOnly();
    }
}
