using MediatR;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetExpenseReport;

/// <summary>
/// Gider raporu — belirtilen tarih araligindaki giderleri kategori ve aylik bazda gruplar.
/// Avalonia ReportsVM bu handler'i kullanarak gider ozetini gosterir.
/// </summary>
public sealed class GetExpenseReportHandler : IRequestHandler<GetExpenseReportQuery, ExpenseReportDto>
{
    private readonly IPersonalExpenseRepository _expenseRepo;
    private readonly ILogger<GetExpenseReportHandler> _logger;

    public GetExpenseReportHandler(
        IPersonalExpenseRepository expenseRepo,
        ILogger<GetExpenseReportHandler> logger)
    {
        _expenseRepo = expenseRepo;
        _logger = logger;
    }

    public async Task<ExpenseReportDto> Handle(GetExpenseReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expenses = await _expenseRepo.GetByDateRangeAsync(
            request.TenantId, request.From, request.To, ct: cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
        {
            expenses = expenses
                .Where(e => string.Equals(e.Category, request.CategoryFilter, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        var totalExpenses = expenses.Sum(e => e.Amount);

        var categoryBreakdown = expenses
            .GroupBy(e => e.Category ?? "Diger")
            .Select(g => new CategoryBreakdownItem
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        var monthlyTrend = expenses
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new MonthlyTrendItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        _logger.LogInformation(
            "Gider raporu olusturuldu: Tenant={TenantId}, From={From}, To={To}, Toplam={Total}",
            request.TenantId, request.From, request.To, totalExpenses);

        return new ExpenseReportDto
        {
            TotalExpenses = totalExpenses,
            CategoryBreakdown = categoryBreakdown.AsReadOnly(),
            MonthlyTrend = monthlyTrend.AsReadOnly()
        };
    }
}
