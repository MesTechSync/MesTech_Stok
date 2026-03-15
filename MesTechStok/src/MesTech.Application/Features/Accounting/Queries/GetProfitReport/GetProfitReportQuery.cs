using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetProfitReport;

public record GetProfitReportQuery(Guid TenantId, string Period, string? Platform = null)
    : IRequest<ProfitReportDto?>;
