using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;

/// <summary>
/// Bilanco dogrulama handler.
/// 1. Aktif hesap planini al
/// 2. AsOfDate tarihine kadar onaylanmis yevmiye satirlarini al
/// 3. BalanceSheetValidationService ile dogrula
/// 4. Sonucu dondur
/// </summary>
public class ValidateBalanceSheetHandler
    : IRequestHandler<ValidateBalanceSheetQuery, BalanceSheetValidationResult>
{
    private readonly IChartOfAccountsRepository _accountRepo;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly BalanceSheetValidationService _validationService;

    public ValidateBalanceSheetHandler(
        IChartOfAccountsRepository accountRepo,
        IJournalEntryRepository journalRepo,
        BalanceSheetValidationService validationService)
    {
        _accountRepo = accountRepo;
        _journalRepo = journalRepo;
        _validationService = validationService;
    }

    public async Task<BalanceSheetValidationResult> Handle(
        ValidateBalanceSheetQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await _accountRepo.GetAllAsync(
            request.TenantId,
            isActive: true,
            cancellationToken);

        var entries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.MinValue,
            request.AsOfDate,
            cancellationToken);

        var postedLines = entries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();

        return _validationService.ValidateFromData(accounts, postedLines);
    }
}
