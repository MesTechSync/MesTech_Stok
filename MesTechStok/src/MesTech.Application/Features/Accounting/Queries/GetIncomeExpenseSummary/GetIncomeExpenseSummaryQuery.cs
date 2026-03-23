using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

public record GetIncomeExpenseSummaryQuery(
    Guid TenantId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<IncomeExpenseSummaryDto>;

public record IncomeExpenseSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    int IncomeCount,
    int ExpenseCount);
