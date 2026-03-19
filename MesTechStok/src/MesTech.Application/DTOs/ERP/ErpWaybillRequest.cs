namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP irsaliye olusturma istegi.
/// </summary>
public record ErpWaybillRequest(
    string CustomerCode,
    string ShippingAddress,
    List<ErpWaybillLineRequest> Lines,
    string? CargoFirm,
    string? TrackingNumber
);

/// <summary>
/// ERP irsaliye satiri.
/// </summary>
public record ErpWaybillLineRequest(
    string ProductCode,
    int Quantity,
    string UnitCode
);
