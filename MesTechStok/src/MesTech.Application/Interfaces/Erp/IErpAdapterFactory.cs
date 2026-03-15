using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP adapter fabrikasi — ErpProvider enum ile adapter resolve eder.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public interface IErpAdapterFactory
{
    /// <summary>
    /// Verilen ERP saglayicisi icin adapter doner.
    /// </summary>
    /// <exception cref="ArgumentException">Desteklenmeyen ERP saglayicisi.</exception>
    IErpAdapter GetAdapter(ErpProvider provider);

    /// <summary>
    /// Desteklenen ERP saglayicilarinin listesi.
    /// </summary>
    IReadOnlyList<ErpProvider> SupportedProviders { get; }
}
