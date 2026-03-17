using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;

public record CreateFixedExpenseCommand(
    Guid TenantId,
    string Name,
    decimal MonthlyAmount,
    int DayOfMonth,
    DateTime StartDate,
    string Currency = "TRY",
    DateTime? EndDate = null,
    string? SupplierName = null,
    Guid? SupplierId = null,
    string? Notes = null
) : IRequest<Guid>;
