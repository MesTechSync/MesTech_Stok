using Mapster;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, GetCustomerByIdResult?>
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdHandler(ICustomerRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<GetCustomerByIdResult?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<GetCustomerByIdResult>();
    }
}
