using Mapster;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetReturnRequestById;

public sealed class GetReturnRequestByIdHandler : IRequestHandler<GetReturnRequestByIdQuery, GetReturnRequestByIdResult?>
{
    private readonly IReturnRequestRepository _repository;

    public GetReturnRequestByIdHandler(IReturnRequestRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<GetReturnRequestByIdResult?> Handle(GetReturnRequestByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<GetReturnRequestByIdResult>();
    }
}
