using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

/// <summary>
/// Envanter degerleme raporu sorgusu.
/// Tum urunlerin stok miktari x birim maliyet ile toplam envanter degerini hesaplar.
/// </summary>
public record InventoryValuationReportQuery(
    Guid TenantId,
    Guid? CategoryFilter = null
) : IRequest<IReadOnlyList<InventoryValuationReportDto>>;
