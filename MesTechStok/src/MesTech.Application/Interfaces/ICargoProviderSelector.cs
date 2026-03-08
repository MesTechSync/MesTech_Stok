using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Siparis icin en uygun kargo firmasini secer.
/// Dalga 3: tenant default + musaitlik. Dalga 4: fiyat + bolge + AI.
/// </summary>
public interface ICargoProviderSelector
{
    Task<CargoProvider> SelectBestProviderAsync(Order order, CancellationToken ct = default);
}
