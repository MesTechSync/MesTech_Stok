using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform adapter fabrikasi — PlatformType veya kod ile adapter resolve eder.
/// </summary>
public interface IAdapterFactory
{
    IIntegratorAdapter? Resolve(PlatformType platformType);
    IIntegratorAdapter? Resolve(string platformCode);
    IReadOnlyList<IIntegratorAdapter> GetAll();
    T? ResolveCapability<T>(string platformCode) where T : class;
}
