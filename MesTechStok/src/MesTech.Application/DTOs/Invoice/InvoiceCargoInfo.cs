namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Invoice Cargo Info data transfer object.
/// </summary>
public record InvoiceCargoInfo(
    string CargoFirm,
    string TrackingNumber,
    string? BarcodeData);
