using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;

public record RecordCargoExpenseCommand(
    Guid TenantId,
    string CarrierName,
    decimal Cost,
    string? OrderId = null,
    string? TrackingNumber = null
) : IRequest<Guid>;
