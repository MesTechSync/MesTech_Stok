using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Kargo adapter fabrikasi — CargoProvider enum ile adapter resolve eder.
/// </summary>
public interface ICargoProviderFactory
{
    ICargoAdapter? Resolve(CargoProvider provider);
    IReadOnlyList<ICargoAdapter> GetAll();
}
