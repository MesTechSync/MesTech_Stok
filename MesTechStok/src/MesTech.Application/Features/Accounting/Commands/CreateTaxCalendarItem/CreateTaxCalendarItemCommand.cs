using MediatR;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Features.Accounting.Commands.CreateTaxCalendarItem;

public record CreateTaxCalendarItemCommand(
    Guid TenantId,
    string TaxType,
    int DueDay,
    int DueMonth,
    string Description,
    TaxCalendarFrequency Frequency,
    bool IsAutoCalculated = false
) : IRequest<Guid>;
