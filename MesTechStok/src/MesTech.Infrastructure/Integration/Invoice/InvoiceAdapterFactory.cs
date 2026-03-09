// NOTE: This is a NEW file — the existing InvoiceProviderFactory.cs is UNTOUCHED.
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// IInvoiceAdapter factory — parallel to InvoiceProviderFactory.
/// Resolves adapters by InvoiceProvider enum.
/// </summary>
public class InvoiceAdapterFactory : IInvoiceAdapterFactory
{
    private readonly Dictionary<InvoiceProvider, IInvoiceAdapter> _adapters;
    private readonly ILogger<InvoiceAdapterFactory> _logger;

    public InvoiceAdapterFactory(
        IEnumerable<IInvoiceAdapter> adapters,
        ILogger<InvoiceAdapterFactory> logger)
    {
        _logger = logger;
        _adapters = adapters.ToDictionary(a => a.Provider.Provider, a => a);
        _logger.LogInformation("InvoiceAdapterFactory baslatildi: {Count} adapter kayitli", _adapters.Count);
    }

    public IInvoiceAdapter? Resolve(InvoiceProvider providerType)
    {
        if (_adapters.TryGetValue(providerType, out var adapter))
            return adapter;

        _logger.LogWarning("InvoiceAdapter bulunamadi: {Provider}", providerType);
        return null;
    }

    public IReadOnlyList<IInvoiceAdapter> GetAll() => _adapters.Values.ToList().AsReadOnly();
}
