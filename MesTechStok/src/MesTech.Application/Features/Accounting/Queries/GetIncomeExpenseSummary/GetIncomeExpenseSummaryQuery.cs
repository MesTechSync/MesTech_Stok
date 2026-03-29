using MediatR;
using MesTech.Application.Behaviors;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

public record GetIncomeExpenseSummaryQuery(
    Guid TenantId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<IncomeExpenseSummaryDto>, ICacheableQuery
{
    public string CacheKey => $"IncomeExpenseSummary_{TenantId}_{From:yyyyMMdd}_{To:yyyyMMdd}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}

public record IncomeExpenseSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    int IncomeCount,
    int ExpenseCount);
