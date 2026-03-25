using MediatR;
using MesTech.Application.DTOs.Finance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Queries.GetBudgetSummary;

public sealed class GetBudgetSummaryHandler : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
{
    private readonly IFinancialGoalRepository _goalRepo;
    private readonly IFinanceExpenseRepository _expenseRepo;

    public GetBudgetSummaryHandler(IFinancialGoalRepository goalRepo, IFinanceExpenseRepository expenseRepo)
        => (_goalRepo, _expenseRepo) = (goalRepo, expenseRepo);

    public async Task<BudgetSummaryDto> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var start = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        // Financial goals as budget targets
        var goals = await _goalRepo.GetActiveAsync(request.TenantId, cancellationToken);
        var activeGoals = goals
            .Where(g => !g.IsDeleted && g.EndDate >= start)
            .ToList();

        var totalBudget = activeGoals.Sum(g => g.TargetAmount);

        // Actual spending
        var expenses = await _expenseRepo.GetByTenantAsync(request.TenantId, null, cancellationToken);
        var periodExpenses = expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end
                     && e.Status != ExpenseStatus.Rejected
                     && e.Status != ExpenseStatus.Draft)
            .ToList();

        var totalSpent = periodExpenses.Sum(e => e.Amount);

        var categories = periodExpenses
            .GroupBy(e => e.Category.ToString(), StringComparer.Ordinal)
            .Select(g => new BudgetCategoryDto
            {
                Category = g.Key,
                Spent = g.Sum(e => e.Amount),
                Budget = totalBudget > 0 && totalSpent > 0
                    ? Math.Round(totalBudget * (g.Sum(e => e.Amount) / totalSpent), 2)
                    : 0
            })
            .OrderByDescending(c => c.Spent)
            .ToList();

        return new BudgetSummaryDto
        {
            Year = request.Year,
            Month = request.Month,
            TotalBudget = totalBudget,
            TotalSpent = totalSpent,
            Categories = categories
        };
    }
}
