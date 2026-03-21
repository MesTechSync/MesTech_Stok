using MediatR;
using MesTech.Application.DTOs.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Queries.GetCashFlow;

public class GetCashFlowHandler : IRequestHandler<GetCashFlowQuery, CashFlowDto>
{
    private readonly IFinanceExpenseRepository _expenseRepo;
    private readonly IOrderRepository _orderRepo;

    public GetCashFlowHandler(IFinanceExpenseRepository expenseRepo, IOrderRepository orderRepo)
        => (_expenseRepo, _orderRepo) = (expenseRepo, orderRepo);

    public async Task<CashFlowDto> Handle(GetCashFlowQuery request, CancellationToken cancellationToken)
    {
        var start = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        var orders = await _orderRepo.GetByDateRangeAsync(start, end);
        var tenantOrders = orders
            .Where(o => o.TenantId == request.TenantId && o.Status != OrderStatus.Cancelled)
            .ToList();

        var totalInflows = tenantOrders.Sum(o => o.TotalAmount);

        var expenses = await _expenseRepo.GetByTenantAsync(request.TenantId, null, cancellationToken);
        var periodExpenses = expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end
                     && e.Status != ExpenseStatus.Rejected
                     && e.Status != ExpenseStatus.Draft)
            .ToList();

        var totalOutflows = periodExpenses.Sum(e => e.Amount);

        var items = new List<CashFlowItemDto>();

        // Inflows by platform
        foreach (var g in tenantOrders.GroupBy(o => o.SourcePlatform?.ToString() ?? "Diger", StringComparer.Ordinal))
        {
            items.Add(new CashFlowItemDto
            {
                Category = g.Key,
                Amount = g.Sum(o => o.TotalAmount),
                IsInflow = true
            });
        }

        // Outflows by expense category
        foreach (var g in periodExpenses.GroupBy(e => e.Category.ToString(), StringComparer.Ordinal))
        {
            items.Add(new CashFlowItemDto
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                IsInflow = false
            });
        }

        return new CashFlowDto
        {
            Year = request.Year,
            Month = request.Month,
            TotalInflows = totalInflows,
            TotalOutflows = totalOutflows,
            Items = items.OrderByDescending(i => i.Amount).ToList()
        };
    }
}
