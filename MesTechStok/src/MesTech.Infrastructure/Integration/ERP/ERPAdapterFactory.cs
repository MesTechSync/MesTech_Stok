using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Factory for resolving ERP adapters.
/// Implements both legacy IERPAdapterFactory (name-based) and Dalga 11 IErpAdapterFactory (enum-based).
/// Dictionary-based, currently supports: "Parasut", "Logo", "BizimHesap".
/// </summary>
public sealed class ERPAdapterFactory : IERPAdapterFactory, IErpAdapterFactory
{
    private readonly Dictionary<string, IERPAdapter> _adapters;
    private readonly Dictionary<ErpProvider, IErpAdapter> _erpAdapters;

    public ERPAdapterFactory(
        IEnumerable<IERPAdapter> adapters,
        IEnumerable<IErpAdapter> erpAdapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(erpAdapters);

        _adapters = new Dictionary<string, IERPAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in adapters)
        {
            _adapters[adapter.ERPName] = adapter;
        }

        _erpAdapters = new Dictionary<ErpProvider, IErpAdapter>();
        foreach (var adapter in erpAdapters)
        {
            _erpAdapters[adapter.Provider] = adapter;
        }
    }

    // ── IERPAdapterFactory (legacy, name-based) ──

    public IERPAdapter GetAdapter(string erpName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(erpName);

        if (_adapters.TryGetValue(erpName, out var adapter))
            return adapter;

        var supported = string.Join(", ", _adapters.Keys.OrderBy(k => k));
        throw new ArgumentException(
            $"Unsupported ERP system: '{erpName}'. Supported ERPs: {supported}",
            nameof(erpName));
    }

    public IReadOnlyList<string> SupportedERPs
        => _adapters.Keys.OrderBy(k => k).ToList().AsReadOnly();

    // ── IErpAdapterFactory (Dalga 11, enum-based) ──

    public IErpAdapter GetAdapter(ErpProvider provider)
    {
        if (provider == ErpProvider.None)
            throw new ArgumentException("ErpProvider.None is not a valid provider.", nameof(provider));

        if (_erpAdapters.TryGetValue(provider, out var adapter))
            return adapter;

        var supported = string.Join(", ", _erpAdapters.Keys.OrderBy(k => k));
        throw new ArgumentException(
            $"Unsupported ERP provider: '{provider}'. Supported providers: {supported}",
            nameof(provider));
    }

    public IReadOnlyList<ErpProvider> SupportedProviders
        => _erpAdapters.Keys.OrderBy(k => k).ToList().AsReadOnly();
}
