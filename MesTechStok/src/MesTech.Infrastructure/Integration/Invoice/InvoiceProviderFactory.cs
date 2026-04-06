using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Fatura provider fabrikasi — InvoiceProvider enum ile provider resolve eder.
/// CargoProviderFactory pattern'ini takip eder.
/// </summary>
public sealed class InvoiceProviderFactory : IInvoiceProviderFactory
{
    private readonly Dictionary<InvoiceProvider, IInvoiceProvider> _providers;
    private readonly ILogger<InvoiceProviderFactory> _logger;

    public InvoiceProviderFactory(
        IEnumerable<IInvoiceProvider> providers,
        ILogger<InvoiceProviderFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(providers);
        _providers = new Dictionary<InvoiceProvider, IInvoiceProvider>();
        foreach (var provider in providers)
            _providers[provider.Provider] = provider;

        _logger.LogInformation("InvoiceProviderFactory initialized with {Count} providers: [{Providers}]",
            _providers.Count, string.Join(", ", _providers.Keys));
    }

    public IInvoiceProvider? Resolve(InvoiceProvider providerType)
    {
        _providers.TryGetValue(providerType, out var provider);
        if (provider is null)
            _logger.LogWarning("InvoiceProviderFactory: No provider found for type '{ProviderType}'", providerType);
        return provider;
    }

    public IReadOnlyList<IInvoiceProvider> GetAll()
        => _providers.Values.ToList().AsReadOnly();
}
