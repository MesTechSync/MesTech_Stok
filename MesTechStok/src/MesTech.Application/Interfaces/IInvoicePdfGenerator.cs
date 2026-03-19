namespace MesTech.Application.Interfaces;

/// <summary>
/// Fatura PDF üretici. QuestPDF veya başka engine ile implementasyon.
/// e-Fatura, e-İrsaliye, e-SMM için ayrı şablonlar.
/// </summary>
public interface IInvoicePdfGenerator
{
    /// <summary>Tek fatura PDF oluştur.</summary>
    Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfRequest request, CancellationToken ct = default);

    /// <summary>e-İrsaliye PDF oluştur (KDV bölümü yok).</summary>
    Task<byte[]> GenerateWaybillPdfAsync(InvoicePdfRequest request, CancellationToken ct = default);

    /// <summary>e-SMM PDF oluştur (stopaj satırı dahil).</summary>
    Task<byte[]> GenerateESMMPdfAsync(InvoicePdfRequest request, CancellationToken ct = default);

    /// <summary>Toplu PDF oluştur (her fatura ayrı sayfa).</summary>
    Task<byte[]> GenerateBulkPdfAsync(List<InvoicePdfRequest> requests, CancellationToken ct = default);
}

/// <summary>PDF üretimi için gerekli fatura bilgileri.</summary>
public record InvoicePdfRequest(
    string InvoiceNumber,
    string InvoiceType,
    DateTime InvoiceDate,
    string SellerName,
    string SellerVkn,
    string SellerTaxOffice,
    string SellerAddress,
    string BuyerName,
    string? BuyerVkn,
    string? BuyerTaxOffice,
    string BuyerAddress,
    string Currency,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? GibUuid,
    List<InvoiceLinePdfItem> Lines,
    // e-İrsaliye
    string? DriverName = null,
    string? VehiclePlate = null,
    DateTime? ShipmentDate = null,
    string? ShipmentAddress = null,
    // e-SMM
    string? ProfessionalTitle = null,
    string? ActivityCode = null,
    decimal? WithholdingRate = null,
    decimal? WithholdingAmount = null);

public record InvoiceLinePdfItem(
    int LineNumber,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal VatAmount,
    decimal LineTotal);
