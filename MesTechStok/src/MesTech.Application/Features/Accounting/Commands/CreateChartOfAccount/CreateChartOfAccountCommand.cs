using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;

public record CreateChartOfAccountCommand(
    Guid TenantId,
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentId = null,
    int Level = 1
) : IRequest<Guid>;
