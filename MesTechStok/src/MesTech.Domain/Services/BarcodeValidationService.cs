namespace MesTech.Domain.Services;

/// <summary>
/// Barkod doğrulama domain servisi — saf iş kuralları.
/// </summary>
public class BarcodeValidationService
{
    /// <summary>
    /// EAN-13 check digit doğrulaması.
    /// </summary>
    public bool ValidateEAN13(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 13 || !barcode.All(char.IsDigit))
            return false;

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = barcode[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (barcode[12] - '0');
    }

    /// <summary>
    /// EAN-8 check digit doğrulaması.
    /// </summary>
    public bool ValidateEAN8(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 8 || !barcode.All(char.IsDigit))
            return false;

        var sum = 0;
        for (var i = 0; i < 7; i++)
        {
            var digit = barcode[i] - '0';
            sum += i % 2 == 0 ? digit * 3 : digit;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (barcode[7] - '0');
    }

    /// <summary>
    /// Barkod formatını algılar.
    /// </summary>
    public string DetectFormat(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return "Unknown";

        return barcode.Length switch
        {
            8 when barcode.All(char.IsDigit) => "EAN8",
            12 when barcode.All(char.IsDigit) => "UPC",
            13 when barcode.All(char.IsDigit) => "EAN13",
            14 when barcode.All(char.IsDigit) => "GTIN14",
            _ => "Code128"
        };
    }
}
