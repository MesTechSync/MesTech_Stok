using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetIncomeById;

public sealed class GetIncomeByIdHandler : IRequestHandler<GetIncomeByIdQuery, IncomeDto?>
{
    private readonly IIncomeRepository _repository;

    public GetIncomeByIdHandler(IIncomeRepository repository)
        => _repository = repository;

    public async Task<IncomeDto?> Handle(GetIncomeByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var income = await _repository.GetByIdAsync(request.Id).ConfigureAwait(false);
        return income?.Adapt<IncomeDto>();
    }
}
