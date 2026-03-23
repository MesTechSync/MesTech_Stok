using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;

public record CloseAccountingPeriodCommand(
    Guid TenantId,
    int Year,
    int Month,
    string UserId) : IRequest<Guid>;
