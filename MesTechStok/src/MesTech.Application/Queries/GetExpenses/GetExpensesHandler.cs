using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetExpenses;

public sealed class GetExpensesHandler : IRequestHandler<GetExpensesQuery, IReadOnlyList<ExpenseDto>>
{
    private readonly IExpenseRepository _expenseRepository;

    public GetExpensesHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<IReadOnlyList<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Type.HasValue)
        {
            var byType = await _expenseRepository.GetByTypeAsync(request.Type.Value, request.TenantId, cancellationToken).ConfigureAwait(false);
            return byType.Adapt<List<ExpenseDto>>().AsReadOnly();
        }

        if (request.From.HasValue && request.To.HasValue)
        {
            var byRange = await _expenseRepository.GetByDateRangeAsync(request.From.Value, request.To.Value, request.TenantId, cancellationToken).ConfigureAwait(false);
            return byRange.Adapt<List<ExpenseDto>>().AsReadOnly();
        }

        var all = await _expenseRepository.GetAllAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return all.Adapt<List<ExpenseDto>>().AsReadOnly();
    }
}
