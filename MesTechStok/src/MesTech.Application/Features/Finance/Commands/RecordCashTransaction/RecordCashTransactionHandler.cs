using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.RecordCashTransaction;

public class RecordCashTransactionHandler : IRequestHandler<RecordCashTransactionCommand, Guid>
{
    private readonly ICashRegisterRepository _cashRegisterRepo;
    private readonly IUnitOfWork _uow;

    public RecordCashTransactionHandler(ICashRegisterRepository cashRegisterRepo, IUnitOfWork uow)
        => (_cashRegisterRepo, _uow) = (cashRegisterRepo, uow);

    public async Task<Guid> Handle(RecordCashTransactionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cashRegister = await _cashRegisterRepo.GetByIdAsync(request.CashRegisterId, cancellationToken)
            ?? throw new InvalidOperationException($"Kasa bulunamadi: {request.CashRegisterId}");

        CashTransaction tx = request.Type switch
        {
            CashTransactionType.Income => cashRegister.RecordIncome(request.Amount, request.Description, request.Category),
            CashTransactionType.Expense => cashRegister.RecordExpense(request.Amount, request.Description, request.Category),
            _ => throw new InvalidOperationException($"Desteklenmeyen islem tipi: {request.Type}")
        };

        await _cashRegisterRepo.UpdateAsync(cashRegister, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return tx.Id;
    }
}
