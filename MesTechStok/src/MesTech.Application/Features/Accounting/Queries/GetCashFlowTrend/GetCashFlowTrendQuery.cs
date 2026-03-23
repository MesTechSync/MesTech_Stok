using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;

public record GetCashFlowTrendQuery(
    Guid TenantId,
    int Months = 6
) : IRequest<CashFlowTrendDto>;

public record CashFlowMonthDto(string Month, decimal Income, decimal Expense, decimal Net);

public record CashFlowTrendDto(IReadOnlyList<CashFlowMonthDto> Months, decimal CumulativeNet);
