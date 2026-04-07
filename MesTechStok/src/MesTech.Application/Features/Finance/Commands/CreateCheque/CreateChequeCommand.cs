using MediatR;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Application.Features.Finance.Commands.CreateCheque;

public record CreateChequeCommand(
    Guid TenantId,
    string ChequeNumber,
    decimal Amount,
    DateTime IssueDate,
    DateTime MaturityDate,
    string BankName,
    ChequeType Type,
    string? DrawerName = null
) : IRequest<Guid>;
