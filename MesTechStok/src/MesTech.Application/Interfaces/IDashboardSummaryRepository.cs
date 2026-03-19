using MesTech.Application.DTOs.Dashboard;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Unified dashboard özet verisi sağlar — 12 KPI + 2 chart + 2 tablo.
/// Implementation: MesTech.Infrastructure / DashboardSummaryRepository (AppDbContext).
/// </summary>
public interface IDashboardSummaryRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken ct = default);
}
