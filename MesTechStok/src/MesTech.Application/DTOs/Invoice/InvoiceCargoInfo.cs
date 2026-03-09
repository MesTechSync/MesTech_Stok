namespace MesTech.Application.DTOs.Invoice;

public record InvoiceCargoInfo(
    string CargoFirm,
    string TrackingNumber,
    string? BarcodeData);
