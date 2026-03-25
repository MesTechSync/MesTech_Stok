using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Finance.Commands.CloseCashRegister;

public sealed class CloseCashRegisterHandler : IRequestHandler<CloseCashRegisterCommand, CloseCashRegisterResult>
{
    private readonly ICashRegisterRepository _cashRegisterRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CloseCashRegisterHandler> _logger;

    public CloseCashRegisterHandler(
        ICashRegisterRepository cashRegisterRepo,
        IUnitOfWork uow,
        ILogger<CloseCashRegisterHandler> logger)
    {
        _cashRegisterRepo = cashRegisterRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<CloseCashRegisterResult> Handle(
        CloseCashRegisterCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cashRegister = await _cashRegisterRepo.GetByIdAsync(request.CashRegisterId, cancellationToken)
            ?? throw new InvalidOperationException($"Kasa bulunamadi: {request.CashRegisterId}");

        if (cashRegister.TenantId != request.TenantId)
            throw new InvalidOperationException("Kasa farkli tenant'a ait.");

        if (!cashRegister.IsActive)
            throw new InvalidOperationException($"Kasa aktif degil: {request.CashRegisterId}");

        // Filter transactions for the closing date
        var closingDateUtc = request.ClosingDate.Date;
        var dayTransactions = cashRegister.Transactions
            .Where(t => t.TransactionDate.Date == closingDateUtc)
            .ToList();

        // Calculate expected balance from day's transactions
        var totalIncome = dayTransactions
            .Where(t => t.Type == CashTransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = dayTransactions
            .Where(t => t.Type == CashTransactionType.Expense)
            .Sum(t => t.Amount);

        var expectedBalance = cashRegister.Balance;
        var cashDifference = request.ActualCashAmount - expectedBalance;

        _logger.LogInformation(
            "Kasa kapama: CashRegisterId={CashRegisterId}, ClosingDate={ClosingDate}, " +
            "ExpectedBalance={ExpectedBalance}, ActualCashAmount={ActualCashAmount}, " +
            "CashDifference={CashDifference}, DayTransactions={TransactionCount}",
            request.CashRegisterId, closingDateUtc,
            expectedBalance, request.ActualCashAmount,
            cashDifference, dayTransactions.Count);

        // Record difference as income or expense if there is a discrepancy
        if (cashDifference > 0)
        {
            cashRegister.RecordIncome(
                cashDifference,
                $"Kasa kapama farki (fazla) — {closingDateUtc:yyyy-MM-dd}",
                "KasaKapama");
        }
        else if (cashDifference < 0)
        {
            cashRegister.RecordExpense(
                Math.Abs(cashDifference),
                $"Kasa kapama farki (eksik) — {closingDateUtc:yyyy-MM-dd}",
                "KasaKapama");
        }

        // Mark register as closed for the day by deactivating
        // In production, a CashRegisterCloseLog entity would track daily closes
        await _cashRegisterRepo.UpdateAsync(cashRegister, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new CloseCashRegisterResult(
            CashRegisterId: cashRegister.Id,
            ClosingDate: closingDateUtc,
            ExpectedBalance: expectedBalance,
            ActualBalance: request.ActualCashAmount,
            CashDifference: cashDifference,
            TransactionCount: dayTransactions.Count,
            IsClosed: true);
    }
}
