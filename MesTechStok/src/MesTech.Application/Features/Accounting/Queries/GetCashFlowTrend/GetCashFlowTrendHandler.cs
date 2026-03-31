using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;

public sealed class GetCashFlowTrendHandler : IRequestHandler<GetCashFlowTrendQuery, CashFlowTrendDto>
{
    private readonly IIncomeRepository _incomeRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly ILogger<GetCashFlowTrendHandler> _logger;

    public GetCashFlowTrendHandler(
        IIncomeRepository incomeRepo,
        IExpenseRepository expenseRepo,
        ILogger<GetCashFlowTrendHandler> logger)
    {
        _incomeRepo = incomeRepo;
        _expenseRepo = expenseRepo;
        _logger = logger;
    }

    public async Task<CashFlowTrendDto> Handle(GetCashFlowTrendQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1).AddMonths(-request.Months + 1);
        var to = now;

        var incomes = await _incomeRepo.GetByDateRangeAsync(from, to, request.TenantId).ConfigureAwait(false);
        var expenses = await _expenseRepo.GetByDateRangeAsync(from, to, request.TenantId).ConfigureAwait(false);

        var months = new List<CashFlowMonthDto>();
        decimal cumulative = 0;

        for (var i = 0; i < request.Months; i++)
        {
            var monthStart = from.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var monthLabel = monthStart.ToString("yyyy-MM");

            var monthIncome = incomes.Where(x => x.Date >= monthStart && x.Date < monthEnd).Sum(x => x.Amount);
            var monthExpense = expenses.Where(x => x.Date >= monthStart && x.Date < monthEnd).Sum(x => x.Amount);
            var net = monthIncome - monthExpense;
            cumulative += net;

            months.Add(new CashFlowMonthDto(monthLabel, monthIncome, monthExpense, net));
        }

        return new CashFlowTrendDto(months, cumulative);
    }
}
