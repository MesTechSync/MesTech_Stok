using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;

public class GetAccountingExpensesHandler : IRequestHandler<GetAccountingExpensesQuery, IReadOnlyList<AccountingExpenseDto>>
{
    private readonly IPersonalExpenseRepository _repository;

    public GetAccountingExpensesHandler(IPersonalExpenseRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<AccountingExpenseDto>> Handle(GetAccountingExpensesQuery request, CancellationToken cancellationToken)
    {
        var expenses = await _repository.GetByDateRangeAsync(request.TenantId, request.From, request.To, request.Source, cancellationToken);
        return expenses.Select(e => new AccountingExpenseDto
        {
            Id = e.Id,
            Title = e.Title,
            Amount = e.Amount,
            Category = e.Category,
            ExpenseDate = e.ExpenseDate,
            Source = e.Source.ToString(),
            IsApproved = e.IsApproved,
            ApprovedBy = e.ApprovedBy
        }).ToList().AsReadOnly();
    }
}
