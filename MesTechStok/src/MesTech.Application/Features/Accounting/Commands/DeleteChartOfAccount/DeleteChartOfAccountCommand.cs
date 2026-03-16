using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;

public record DeleteChartOfAccountCommand(
    Guid Id,
    string DeletedBy = "system"
) : IRequest<bool>;
