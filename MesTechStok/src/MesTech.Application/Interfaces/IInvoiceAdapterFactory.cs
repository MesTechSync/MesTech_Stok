using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// InvoiceProvider enum ile IInvoiceAdapter resolve eder.
/// IInvoiceProviderFactory'ye paralel — eski factory dokunulmaz.
/// </summary>
public interface IInvoiceAdapterFactory
{
    IInvoiceAdapter? Resolve(InvoiceProvider providerType);
    IReadOnlyList<IInvoiceAdapter> GetAll();
}
