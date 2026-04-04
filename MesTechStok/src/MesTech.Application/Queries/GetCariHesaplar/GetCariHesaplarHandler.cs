using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetCariHesaplar;

public sealed class GetCariHesaplarHandler : IRequestHandler<GetCariHesaplarQuery, IReadOnlyList<CariHesapDto>>
{
    private readonly ICariHesapRepository _cariHesapRepository;

    public GetCariHesaplarHandler(ICariHesapRepository cariHesapRepository)
    {
        _cariHesapRepository = cariHesapRepository;
    }

    public async Task<IReadOnlyList<CariHesapDto>> Handle(GetCariHesaplarQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Type.HasValue)
        {
            var byType = await _cariHesapRepository.GetByTypeAsync(request.Type.Value, request.TenantId, cancellationToken).ConfigureAwait(false);
            return byType.Adapt<List<CariHesapDto>>().AsReadOnly();
        }

        var all = await _cariHesapRepository.GetAllAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return all.Adapt<List<CariHesapDto>>().AsReadOnly();
    }
}
