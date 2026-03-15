using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;

public record GetCashFlowReportQuery(Guid TenantId, DateTime From, DateTime To)
    : IRequest<CashFlowReportDto>;
