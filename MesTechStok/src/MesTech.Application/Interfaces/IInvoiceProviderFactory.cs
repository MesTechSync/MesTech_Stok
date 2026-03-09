using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Fatura provider fabrikasi — InvoiceProvider enum ile provider resolve eder.
/// </summary>
public interface IInvoiceProviderFactory
{
    IInvoiceProvider? Resolve(InvoiceProvider providerType);
    IReadOnlyList<IInvoiceProvider> GetAll();
}
