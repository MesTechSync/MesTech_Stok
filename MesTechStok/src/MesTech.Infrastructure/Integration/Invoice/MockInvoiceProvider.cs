using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Mock IInvoiceProvider — returns realistic sample data so the Fatura Yonetimi
/// screen is functional. Will be replaced by real Sovos/Parasut providers in production.
/// </summary>
public sealed class MockInvoiceProvider : IInvoiceProvider
{
    public string ProviderName => "Mock e-Fatura (Test)";
    public InvoiceProvider Provider => InvoiceProvider.Manual;

    public Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        var gibId = $"GIB{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        return Task.FromResult(new InvoiceResult(true, gibId, null, null));
    }

    public Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        var gibId = $"ARS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        return Task.FromResult(new InvoiceResult(true, gibId, null, null));
    }

    public Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        var gibId = $"IRS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        return Task.FromResult(new InvoiceResult(true, gibId, null, null));
    }

    public Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        return Task.FromResult(new InvoiceStatusResult(gibInvoiceId, "Accepted", DateTime.UtcNow.AddMinutes(-5), null));
    }

    public Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        // Return minimal valid PDF bytes for testing
        var pdfContent = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
                         $"2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
                         $"3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj\n" +
                         $"%%EOF\n";
        return Task.FromResult(System.Text.Encoding.ASCII.GetBytes(pdfContent));
    }

    public Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        // Simulate: tax numbers starting with '3' are e-Invoice taxpayers
        return Task.FromResult(taxNumber.StartsWith("3"));
    }

    public Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        return Task.FromResult(new InvoiceResult(true, gibInvoiceId, null, null));
    }
}
