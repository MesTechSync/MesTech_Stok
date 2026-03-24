using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

public class GetIncomeExpenseSummaryHandler
    : IRequestHandler<GetIncomeExpenseSummaryQuery, IncomeExpenseSummaryDto>
{
    private readonly IIncomeRepository _incomeRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly ILogger<GetIncomeExpenseSummaryHandler> _logger;

    public GetIncomeExpenseSummaryHandler(
        IIncomeRepository incomeRepo,
        IExpenseRepository expenseRepo,
        ILogger<GetIncomeExpenseSummaryHandler> logger)
    {
        _incomeRepo = incomeRepo;
        _expenseRepo = expenseRepo;
        _logger = logger;
    }

    public async Task<IncomeExpenseSummaryDto> Handle(
        GetIncomeExpenseSummaryQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.AddMonths(-1);
        var to = request.To ?? DateTime.UtcNow;

        var incomes = await _incomeRepo.GetByDateRangeAsync(from, to, request.TenantId);
        var expenses = await _expenseRepo.GetByDateRangeAsync(from, to, request.TenantId);

        var totalIncome = incomes.Sum(i => i.Amount);
        var totalExpense = expenses.Sum(e => e.Amount);

        return new IncomeExpenseSummaryDto(
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            NetProfit: totalIncome - totalExpense,
            IncomeCount: incomes.Count,
            ExpenseCount: expenses.Count);
    }
}
