using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;

/// <summary>
/// Bilanco (Balance Sheet) raporu sorgusu.
/// AsOfDate tarihine kadar olan bakiyeleri hesap turune gore gruplar.
/// Turkish THP: 1xx=Varliklar, 2xx-3xx=Borclar, 5xx=Ozkaynaklar.
/// </summary>
public record GetBalanceSheetQuery(Guid TenantId, DateTime AsOfDate)
    : IRequest<BalanceSheetDto>;
