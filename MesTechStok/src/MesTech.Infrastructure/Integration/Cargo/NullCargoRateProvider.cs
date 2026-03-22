using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces.Cargo;

namespace MesTech.Infrastructure.Integration.Cargo;

/// <summary>
/// Stub kargo fiyat provider'i — DEV 3 gercek provider implement edene kadar
/// DI resolution hatasini onler. Her zaman null dondurur.
/// </summary>
public class NullCargoRateProvider : ICargoRateProvider
{
    public Task<CargoRateResult?> GetRateAsync(
        ShipmentRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<CargoRateResult?>(null);
}
