using Mapster;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetCariHesapById;

public sealed class GetCariHesapByIdHandler : IRequestHandler<GetCariHesapByIdQuery, CariHesapDto?>
{
    private readonly ICariHesapRepository _repository;

    public GetCariHesapByIdHandler(ICariHesapRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<CariHesapDto?> Handle(GetCariHesapByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<CariHesapDto>();
    }
}
