using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;

public record CreateFinancialGoalCommand(
    Guid TenantId,
    string Title,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Guid>;
