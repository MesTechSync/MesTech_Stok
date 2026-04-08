using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CalculateVatDeclaration;

/// <summary>
/// KDV beyanname hesaplama — GL yevmiye kayitlarindan 391 ve 191 hesap toplamlarini cekar.
/// </summary>
public sealed class CalculateVatDeclarationHandler : IRequestHandler<CalculateVatDeclarationCommand, VatCalculationResult>
{
    private readonly IVatDeclarationRepository _vatRepo;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly IUnitOfWork _uow;

    public CalculateVatDeclarationHandler(
        IVatDeclarationRepository vatRepo,
        IJournalEntryRepository journalRepo,
        IUnitOfWork uow)
    {
        _vatRepo = vatRepo;
        _journalRepo = journalRepo;
        _uow = uow;
    }

    public async Task<VatCalculationResult> Handle(CalculateVatDeclarationCommand request, CancellationToken ct)
    {
        var declaration = await _vatRepo.GetByIdAsync(request.DeclarationId, ct).ConfigureAwait(false);
        if (declaration is null)
            return new VatCalculationResult { IsSuccess = false, ErrorMessage = "Beyanname bulunamadi." };

        var periodStart = new DateTime(declaration.Year, declaration.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // GL entry'lerden hesap toplamlarini cek
        var entries = await _journalRepo.GetByDateRangeAsync(
            declaration.TenantId, periodStart, periodEnd, ct).ConfigureAwait(false);

        decimal totalSales = 0, vatCollected = 0, vatPaid = 0;

        foreach (var entry in entries)
        {
            foreach (var line in entry.Lines)
            {
                if (line.AccountId == AccountingConstants.Account600DomesticSales)
                    totalSales += line.Credit;
                if (line.AccountId == AccountingConstants.Account391VatPayable)
                    vatCollected += line.Credit;
                if (line.AccountId == AccountingConstants.Account191VatReceivable)
                    vatPaid += line.Debit;
            }
        }

        declaration.Calculate(totalSales, vatCollected, vatPaid);
        await _vatRepo.UpdateAsync(declaration, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new VatCalculationResult
        {
            IsSuccess = true,
            TotalSales = totalSales,
            VatCollected = vatCollected,
            VatPaid = vatPaid,
            NetVatPayable = vatCollected - vatPaid
        };
    }
}
