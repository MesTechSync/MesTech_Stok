using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;

public sealed class GetIncomeExpenseListHandler
    : IRequestHandler<GetIncomeExpenseListQuery, IncomeExpenseListResultDto>
{
    private readonly IIncomeRepository _incomeRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly ILogger<GetIncomeExpenseListHandler> _logger;

    public GetIncomeExpenseListHandler(
        IIncomeRepository incomeRepo,
        IExpenseRepository expenseRepo,
        ILogger<GetIncomeExpenseListHandler> logger)
    {
        _incomeRepo = incomeRepo;
        _expenseRepo = expenseRepo;
        _logger = logger;
    }

    public async Task<IncomeExpenseListResultDto> Handle(
        GetIncomeExpenseListQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.MinValue;
        var to = request.To ?? DateTime.UtcNow;

        var incomes = await _incomeRepo.GetByDateRangeAsync(from, to, request.TenantId).ConfigureAwait(false);
        var expenses = await _expenseRepo.GetByDateRangeAsync(from, to, request.TenantId).ConfigureAwait(false);

        var items = incomes
            .Select(i => new IncomeExpenseItemDto(i.Id, i.Description, i.Amount, "Income", i.Source.ToString(), i.Date, i.OrderId))
            .Concat(expenses.Select(e => new IncomeExpenseItemDto(e.Id, e.Description, e.Amount, "Expense", e.Category.ToString(), e.Date, null)))
            .OrderByDescending(x => x.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new IncomeExpenseListResultDto(items, incomes.Count + expenses.Count);
    }
}
