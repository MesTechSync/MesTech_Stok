using MediatR;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Application.Features.Finance.Commands.RecordCashTransaction;

public record RecordCashTransactionCommand(
    Guid TenantId, Guid CashRegisterId,
    CashTransactionType Type, decimal Amount,
    string Description, string? Category = null,
    Guid? RelatedInvoiceId = null, Guid? RelatedCurrentAccountId = null
) : IRequest<Guid>;
