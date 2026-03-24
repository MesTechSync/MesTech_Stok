using MediatR;

namespace MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;

/// <summary>
/// Tedarikçi performans sorgusu — sipariş istatistikleri ve rating hesaplama.
/// </summary>
public record GetSupplierPerformanceQuery(
    Guid TenantId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<List<SupplierPerformanceDto>>;

/// <summary>
/// Tedarikçi performans özet DTO'su.
/// </summary>
public class SupplierPerformanceDto
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public int TotalOrders { get; init; }
    public int FulfilledOrders { get; init; }
    public int FailedOrders { get; init; }
    public double AverageFulfillmentDays { get; init; }
    public double ReturnRate { get; init; }

    /// <summary>
    /// 1-5 arası hesaplanmış puan.
    /// Fulfillment oranı ağırlıklı (failed düşürür, hız artırır).
    /// </summary>
    public double Rating { get; init; }
}
