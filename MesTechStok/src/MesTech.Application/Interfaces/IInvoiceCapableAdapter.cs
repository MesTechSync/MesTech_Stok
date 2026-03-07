namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform uzerinden fatura gonderebilen adaptörler icin interface.
/// </summary>
public interface IInvoiceCapableAdapter
{
    Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default);
    Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default);
}
