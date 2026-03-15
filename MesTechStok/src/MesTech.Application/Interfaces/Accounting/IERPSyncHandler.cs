namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// ERP senkronizasyon handler'i — fatura ve siparis olaylarina tepki vererek
/// ilgili ERP sistemine veri aktarimi baslatir.
/// </summary>
public interface IERPSyncHandler
{
    /// <summary>
    /// Fatura olusturuldugunda ERP'ye senkronizasyon tetikler.
    /// </summary>
    Task HandleInvoiceCreatedAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Siparis alindigi zaman ERP'ye senkronizasyon tetikler.
    /// </summary>
    Task HandleOrderReceivedAsync(Guid orderId, CancellationToken ct = default);
}
