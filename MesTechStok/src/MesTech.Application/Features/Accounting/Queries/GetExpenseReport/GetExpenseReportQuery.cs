using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetExpenseReport;

public record GetExpenseReportQuery(
    Guid TenantId,
    DateTime From,
    DateTime To,
    string? CategoryFilter = null
) : IRequest<ExpenseReportDto>;
