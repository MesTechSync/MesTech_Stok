namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Barkod Value Object — GS1 Modulo 10 check digit destegi.
/// EAN-8, EAN-13, UPC-A (12), GTIN-14 formatlarini tanir.
/// Turkiye GS1 oneki: 868/869.
/// </summary>
public record Barcode
{
    public string Value { get; }
    public BarcodeFormat Format { get; }

    public Barcode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Barcode cannot be empty.", nameof(value));

        Value = value.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        Format = DetectFormat(Value);
    }

    private Barcode(string value, BarcodeFormat format)
    {
        Value = value;
        Format = format;
    }

    public static Barcode Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var cleaned = value.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        var format = DetectFormat(cleaned);

        if (format != BarcodeFormat.Internal && !ValidateCheckDigit(cleaned))
            throw new ArgumentException($"Gecersiz GS1 kontrol basamagi: {cleaned}", nameof(value));

        return new Barcode(cleaned, format);
    }

    public static Barcode CreateInternal(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new Barcode(value.Trim(), BarcodeFormat.Internal);
    }

    public bool IsEAN13 => Format == BarcodeFormat.EAN13;
    public bool IsEAN8 => Format == BarcodeFormat.EAN8;
    public bool IsUPC => Format == BarcodeFormat.UPCA;
    public bool IsGS1 => Format != BarcodeFormat.Internal;
    public bool IsTurkish => Value.StartsWith("868", StringComparison.Ordinal)
                          || Value.StartsWith("869", StringComparison.Ordinal);
    public bool IsCode128 => Value.Length >= 1;

    public string? GetCountryPrefix()
    {
        if (!IsEAN13) return null;
        return Value[..3];
    }

    public static bool ValidateCheckDigit(string barcode)
    {
        if (barcode.Length < 8 || !barcode.All(char.IsDigit)) return false;
        var digits = barcode.Select(c => c - '0').ToArray();
        var sum = 0;
        for (var i = 0; i < digits.Length - 1; i++)
        {
            var weight = ((digits.Length - 1 - i) % 2 == 0) ? 1 : 3;
            sum += digits[i] * weight;
        }
        return (10 - (sum % 10)) % 10 == digits[^1];
    }

    private static BarcodeFormat DetectFormat(string cleaned)
    {
        if (!cleaned.All(char.IsDigit))
            return BarcodeFormat.Internal;

        return cleaned.Length switch
        {
            8 => BarcodeFormat.EAN8,
            12 => BarcodeFormat.UPCA,
            13 => BarcodeFormat.EAN13,
            14 => BarcodeFormat.GTIN14,
            _ => BarcodeFormat.Internal
        };
    }

    public override string ToString() => Value;

    public static implicit operator string(Barcode barcode) => barcode.Value;
}

public enum BarcodeFormat
{
    EAN8,
    EAN13,
    UPCA,
    GTIN14,
    ISBN13,
    Internal
}
