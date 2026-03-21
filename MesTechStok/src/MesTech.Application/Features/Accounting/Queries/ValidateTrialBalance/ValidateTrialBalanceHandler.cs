using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;

/// <summary>
/// Mizan dogrulama handler.
/// 1. Donem icindeki onaylanmis yevmiye kayitlarini al
/// 2. TrialBalanceValidationService ile dogrula
/// 3. Sonucu dondur
/// </summary>
public class ValidateTrialBalanceHandler
    : IRequestHandler<ValidateTrialBalanceQuery, TrialBalanceValidationResult>
{
    private readonly IJournalEntryRepository _journalRepo;
    private readonly TrialBalanceValidationService _validationService;

    public ValidateTrialBalanceHandler(
        IJournalEntryRepository journalRepo,
        TrialBalanceValidationService validationService)
    {
        _journalRepo = journalRepo;
        _validationService = validationService;
    }

    public async Task<TrialBalanceValidationResult> Handle(
        ValidateTrialBalanceQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return _validationService.ValidateFromEntries(entries);
    }
}
