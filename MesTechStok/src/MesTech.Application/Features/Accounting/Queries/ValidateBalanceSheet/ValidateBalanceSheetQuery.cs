using MediatR;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;

/// <summary>
/// Bilanco dogrulama sorgusu.
/// Belirli bir tarih itibariyle Assets == Liabilities + Equity kuralini dogrular.
/// </summary>
public record ValidateBalanceSheetQuery(
    Guid TenantId,
    DateTime AsOfDate) : IRequest<BalanceSheetValidationResult>;
