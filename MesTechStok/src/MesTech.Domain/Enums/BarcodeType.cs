namespace MesTech.Domain.Enums;

/// <summary>
/// Barkod formatı.
/// </summary>
public enum BarcodeType
{
    EAN13 = 0,
    EAN8 = 1,
    UPC = 2,
    GTIN = 3,
    Code128 = 4,
    Code39 = 5,
    QR = 6
}
