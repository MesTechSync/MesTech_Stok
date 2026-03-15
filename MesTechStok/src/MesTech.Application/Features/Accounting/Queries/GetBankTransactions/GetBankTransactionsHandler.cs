using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetBankTransactions;

public class GetBankTransactionsHandler : IRequestHandler<GetBankTransactionsQuery, IReadOnlyList<BankTransactionDto>>
{
    private readonly IBankTransactionRepository _repository;

    public GetBankTransactionsHandler(IBankTransactionRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<BankTransactionDto>> Handle(GetBankTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _repository.GetByBankAccountAsync(
            request.TenantId, request.BankAccountId, request.From, request.To, cancellationToken);

        return transactions.Select(t => new BankTransactionDto
        {
            Id = t.Id,
            BankAccountId = t.BankAccountId,
            TransactionDate = t.TransactionDate,
            Amount = t.Amount,
            Description = t.Description,
            ReferenceNumber = t.ReferenceNumber,
            IsReconciled = t.IsReconciled
        }).ToList().AsReadOnly();
    }
}
