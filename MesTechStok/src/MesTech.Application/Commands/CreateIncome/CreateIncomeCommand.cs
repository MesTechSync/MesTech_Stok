using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.CreateIncome;

public record CreateIncomeCommand(
    Guid TenantId,
    Guid? StoreId,
    string Description,
    decimal Amount,
    IncomeType IncomeType,
    Guid? InvoiceId,
    DateTime? Date,
    string? Note
) : IRequest<Guid>;
