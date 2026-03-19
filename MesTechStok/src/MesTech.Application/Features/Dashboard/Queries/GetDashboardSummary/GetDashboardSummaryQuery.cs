using MesTech.Application.DTOs.Dashboard;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Unified 12-KPI dashboard özet sorgusu.
/// Mevcut 6 dashboard query'ye DOKUNMAZ — ek aggregation katmanı.
/// </summary>
public record GetDashboardSummaryQuery(Guid TenantId) : IRequest<DashboardSummaryDto>;
