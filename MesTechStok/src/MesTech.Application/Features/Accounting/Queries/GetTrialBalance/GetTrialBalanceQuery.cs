using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTrialBalance;

/// <summary>
/// Mizan (Trial Balance) raporu sorgusu.
/// StartDate-EndDate araligi icin acilis, donem ve kapanis bakiyelerini hesaplar.
/// </summary>
public record GetTrialBalanceQuery(Guid TenantId, DateTime StartDate, DateTime EndDate)
    : IRequest<TrialBalanceDto>;
