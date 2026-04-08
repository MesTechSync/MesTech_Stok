#pragma warning disable MA0051 // Method is too long — monthly summary handler is a single cohesive aggregation
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;

/// <summary>
/// Aylik ozet raporu handler.
/// Order + Expense + Income verilerinden aylik metrikleri hesaplar.
/// </summary>
public sealed class GetMonthlySummaryHandler : IRequestHandler<GetMonthlySummaryQuery, MonthlySummaryDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly IIncomeRepository _incomeRepo;

    public GetMonthlySummaryHandler(
        IOrderRepository orderRepo,
        IExpenseRepository expenseRepo,
        IIncomeRepository incomeRepo)
    {
        _orderRepo = orderRepo;
        _expenseRepo = expenseRepo;
        _incomeRepo = incomeRepo;
    }

    public async Task<MonthlySummaryDto> Handle(
        GetMonthlySummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var start = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        // Orders for the period (tenant-scoped)
        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId, start, end, cancellationToken);

        var activeOrders = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .ToList();

        var cancelledOrders = orders
            .Where(o => o.Status == OrderStatus.Cancelled)
            .ToList();

        var totalSales = activeOrders.Sum(o => o.TotalAmount);
        var totalTax = activeOrders.Sum(o => o.TaxAmount);
        var totalOrders = activeOrders.Count;
        var totalReturns = cancelledOrders.Count;

        // Expenses for the period
        var expenses = await _expenseRepo.GetByDateRangeAsync(
            start, end, request.TenantId, cancellationToken);

        var totalExpenses = expenses.Sum(e => e.Amount);

        var totalCommissions = expenses
            .Where(e => e.ExpenseType == ExpenseType.Komisyon)
            .Sum(e => e.Amount);

        var totalShippingCost = expenses
            .Where(e => e.ExpenseType == ExpenseType.Kargo)
            .Sum(e => e.Amount);

        // Sales by platform
        var salesByPlatform = activeOrders
            .GroupBy(o => o.SourcePlatform?.ToString() ?? "Diger", StringComparer.Ordinal)
            .Select(g =>
            {
                var platformExpenses = expenses
                    .Where(e => e.ExpenseType == ExpenseType.Komisyon)
                    .ToList();

                return new PlatformSalesDto
                {
                    Platform = g.Key,
                    Sales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    Commission = Math.Round(
                        totalCommissions > 0 && totalSales > 0
                            ? totalCommissions * (g.Sum(o => o.TotalAmount) / totalSales)
                            : 0, 2)
                };
            })
            .OrderByDescending(p => p.Sales)
            .ToList();

        return new MonthlySummaryDto
        {
            Year = request.Year,
            Month = request.Month,
            TotalSales = totalSales,
            TotalCommissions = totalCommissions,
            TotalShippingCost = totalShippingCost,
            TotalExpenses = totalExpenses,
            TotalTaxDue = totalTax,
            TotalOrders = totalOrders,
            TotalReturns = totalReturns,
            SalesByPlatform = salesByPlatform
        };
    }
}
