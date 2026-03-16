using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;

public record UpdateChartOfAccountCommand(
    Guid Id,
    string Name
) : IRequest<bool>;
