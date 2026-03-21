using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetCounterparties;

public class GetCounterpartiesHandler : IRequestHandler<GetCounterpartiesQuery, IReadOnlyList<CounterpartyDto>>
{
    private readonly ICounterpartyRepository _repository;

    public GetCounterpartiesHandler(ICounterpartyRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<CounterpartyDto>> Handle(GetCounterpartiesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = await _repository.GetAllAsync(request.TenantId, request.Type, request.IsActive, cancellationToken);
        return items.Select(c => new CounterpartyDto
        {
            Id = c.Id,
            Name = c.Name,
            VKN = c.VKN,
            CounterpartyType = c.CounterpartyType.ToString(),
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            Platform = c.Platform,
            IsActive = c.IsActive
        }).ToList().AsReadOnly();
    }
}
