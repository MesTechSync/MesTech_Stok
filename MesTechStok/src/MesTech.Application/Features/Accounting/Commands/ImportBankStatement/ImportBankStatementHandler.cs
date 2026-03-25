using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.ImportBankStatement;

public sealed class ImportBankStatementHandler : IRequestHandler<ImportBankStatementCommand, int>
{
    private readonly IBankTransactionRepository _repository;
    private readonly IUnitOfWork _uow;

    public ImportBankStatementHandler(IBankTransactionRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<int> Handle(ImportBankStatementCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var importedCount = 0;
        var transactions = new List<BankTransaction>();

        foreach (var input in request.Transactions)
        {
            // Idempotency check — skip duplicates
            if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
            {
                var existing = await _repository.GetByIdempotencyKeyAsync(
                    request.TenantId, input.IdempotencyKey, cancellationToken);
                if (existing != null) continue;
            }

            var transaction = BankTransaction.Create(
                request.TenantId, request.BankAccountId, input.TransactionDate,
                input.Amount, input.Description, input.ReferenceNumber, input.IdempotencyKey);

            transactions.Add(transaction);
            importedCount++;
        }

        if (transactions.Count > 0)
        {
            await _repository.AddRangeAsync(transactions, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return importedCount;
    }
}
