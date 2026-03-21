using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;

public class GetFixedExpenseByIdHandler : IRequestHandler<GetFixedExpenseByIdQuery, FixedExpenseDto?>
{
    private readonly IFixedExpenseRepository _repository;

    public GetFixedExpenseByIdHandler(IFixedExpenseRepository repository)
        => _repository = repository;

    public async Task<FixedExpenseDto?> Handle(GetFixedExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return expense?.Adapt<FixedExpenseDto>();
    }
}
