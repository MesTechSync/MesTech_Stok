using Mapster;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetBrandById;

public sealed class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, GetBrandByIdResult?>
{
    private readonly IBrandRepository _repository;

    public GetBrandByIdHandler(IBrandRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<GetBrandByIdResult?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<GetBrandByIdResult>();
    }
}
