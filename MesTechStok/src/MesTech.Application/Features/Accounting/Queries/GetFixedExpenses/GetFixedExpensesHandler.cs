using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;

public sealed class GetFixedExpensesHandler : IRequestHandler<GetFixedExpensesQuery, IReadOnlyList<FixedExpenseDto>>
{
    private readonly IFixedExpenseRepository _repository;

    public GetFixedExpensesHandler(IFixedExpenseRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<FixedExpenseDto>> Handle(GetFixedExpensesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expenses = await _repository.GetAllAsync(request.TenantId, request.IsActive, cancellationToken).ConfigureAwait(false);
        return expenses.Adapt<List<FixedExpenseDto>>().AsReadOnly();
    }
}
