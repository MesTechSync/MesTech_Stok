using MediatR;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;

/// <summary>
/// Mizan dogrulama sorgusu.
/// Belirli bir donem icin sum(debit) == sum(credit) kuralini dogrular.
/// </summary>
public record ValidateTrialBalanceQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<TrialBalanceValidationResult>;
