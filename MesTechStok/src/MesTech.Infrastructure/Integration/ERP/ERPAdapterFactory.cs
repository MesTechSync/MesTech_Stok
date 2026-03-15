using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Factory for resolving ERP adapters by name.
/// Dictionary-based, currently supports: "Parasut".
/// </summary>
public sealed class ERPAdapterFactory : IERPAdapterFactory
{
    private readonly Dictionary<string, IERPAdapter> _adapters;

    public ERPAdapterFactory(IEnumerable<IERPAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        _adapters = new Dictionary<string, IERPAdapter>(StringComparer.OrdinalIgnoreCase);

        foreach (var adapter in adapters)
        {
            _adapters[adapter.ERPName] = adapter;
        }
    }

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
}
