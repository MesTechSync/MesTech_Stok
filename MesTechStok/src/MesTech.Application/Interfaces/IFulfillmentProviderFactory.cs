using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Factory for resolving <see cref="IFulfillmentProvider"/> instances by <see cref="FulfillmentCenter"/>.
/// Infrastructure layer provides the implementation.
/// </summary>
public interface IFulfillmentProviderFactory
{
    /// <summary>
    /// Resolves the provider for a given fulfillment center.
    /// Returns null if no provider is registered for that center.
    /// </summary>
    IFulfillmentProvider? Resolve(FulfillmentCenter center);

    /// <summary>
    /// Returns all registered fulfillment providers.
    /// </summary>
    IReadOnlyList<IFulfillmentProvider> GetAll();
}
