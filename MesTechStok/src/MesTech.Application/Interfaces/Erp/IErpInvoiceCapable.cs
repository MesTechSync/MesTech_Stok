using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP fatura yetkinligi — fatura CRUD islemleri destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpInvoiceCapable
{
    Task<ErpInvoiceResult> CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct = default);
    Task<ErpInvoiceResult?> GetInvoiceAsync(string invoiceNumber, CancellationToken ct = default);
    Task<List<ErpInvoiceResult>> GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct = default);
}
