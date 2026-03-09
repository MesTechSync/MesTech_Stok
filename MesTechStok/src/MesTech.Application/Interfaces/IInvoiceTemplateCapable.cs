using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

public interface IInvoiceTemplateCapable
{
    Task<bool> SetInvoiceTemplateAsync(InvoiceTemplateDto template, CancellationToken ct = default);
}
