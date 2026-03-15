using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Interfaces;

public interface IUblTrXmlBuilder
{
    Task<byte[]> BuildAsync(EInvoiceDocument document, CancellationToken ct = default);
}
