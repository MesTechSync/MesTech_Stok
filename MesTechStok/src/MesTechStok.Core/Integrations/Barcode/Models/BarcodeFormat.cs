namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public enum BarcodeFormat
    {
        Unknown,
        Code128,
        Code39,
        EAN13,
        EAN8,
        UPC_A,
        UPCA = UPC_A, // Alias for compatibility
        UPC_E,
        QR_Code,
        QRCode = QR_Code, // Alias for compatibility
        DataMatrix,
        PDF417,
        Aztec,
        Codabar,
        ITF,
        MSI
    }
}
