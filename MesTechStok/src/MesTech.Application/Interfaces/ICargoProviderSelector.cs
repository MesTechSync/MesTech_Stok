using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Siparis icin en uygun kargo firmasini secer.
/// Phase C: AvailabilityFirst | CheapestFirst | FastestFirst strateji destegi.
/// </summary>
public interface ICargoProviderSelector
{
    /// <summary>
    /// Varsayilan strateji (AvailabilityFirst) ile kargo saglayici secer.
    /// </summary>
    Task<CargoProvider> SelectBestProviderAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen strateji ile kargo saglayici secer.
    /// CheapestFirst/FastestFirst: ICargoRateProvider implement eden adaptorlerden fiyat sorgusu yapar.
    /// Rate provider yoksa AvailabilityFirst'e duser.
    /// </summary>
    Task<CargoProvider> SelectBestProviderAsync(
        Order order,
        CargoSelectionStrategy strategy,
        ShipmentRequest? shipmentRequest = null,
        CancellationToken ct = default);
}
