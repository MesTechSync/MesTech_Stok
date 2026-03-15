using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetBankTransactions;

public record GetBankTransactionsQuery(Guid TenantId, Guid BankAccountId, DateTime? From = null, DateTime? To = null)
    : IRequest<IReadOnlyList<BankTransactionDto>>;
