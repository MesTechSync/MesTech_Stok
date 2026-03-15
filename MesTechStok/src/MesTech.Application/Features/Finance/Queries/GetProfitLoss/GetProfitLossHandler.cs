using MediatR;
using MesTech.Application.DTOs.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Queries.GetProfitLoss;

public class GetProfitLossHandler : IRequestHandler<GetProfitLossQuery, ProfitLossDto>
{
    private readonly IFinanceExpenseRepository _expenseRepo;
    private readonly IOrderRepository _orderRepo;

    public GetProfitLossHandler(IFinanceExpenseRepository expenseRepo, IOrderRepository orderRepo)
        => (_expenseRepo, _orderRepo) = (expenseRepo, orderRepo);

    public async Task<ProfitLossDto> Handle(GetProfitLossQuery req, CancellationToken ct)
    {
        var start = new DateTime(req.Year, req.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        // Revenue from orders (IOrderRepository.GetByDateRangeAsync does not take tenantId)
        var orders = await _orderRepo.GetByDateRangeAsync(start, end);
        var tenantOrders = orders.Where(o => o.TenantId == req.TenantId).ToList();

        var totalRevenue = tenantOrders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Sum(o => o.TotalAmount);

        var revenueByPlatform = tenantOrders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.SourcePlatform?.ToString() ?? "Diger")
            .Select(g => new PlatformRevenueDto
            {
                Platform = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        // Expenses from FinanceExpense (supports tenantId + date range)
        var totalExpenses = await _expenseRepo.GetTotalByDateRangeAsync(
            req.TenantId, start, end, ct);

        var expenses = await _expenseRepo.GetByTenantAsync(req.TenantId, null, ct);
        var expenseByCategory = expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end
                     && e.Status != ExpenseStatus.Rejected
                     && e.Status != ExpenseStatus.Draft)
            .GroupBy(e => e.Category.ToString())
            .Select(g => new ExpenseCategoryDto
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        return new ProfitLossDto
        {
            Year = req.Year,
            Month = req.Month,
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            RevenueByPlatform = revenueByPlatform,
            ExpenseByCategory = expenseByCategory
        };
    }
}
