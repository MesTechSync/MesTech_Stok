namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Barkod doğrulama mantığı içeren value object.
/// </summary>
public record Barcode
{
    public string Value { get; }

    public Barcode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Barcode cannot be empty.", nameof(value));

        Value = value.Trim();
    }

    public bool IsEAN13 => Value.Length == 13 && Value.All(char.IsDigit);
    public bool IsEAN8 => Value.Length == 8 && Value.All(char.IsDigit);
    public bool IsUPC => Value.Length == 12 && Value.All(char.IsDigit);
    public bool IsCode128 => Value.Length >= 1;

    public string? GetCountryPrefix()
    {
        if (!IsEAN13) return null;
        return Value[..3];
    }

    public override string ToString() => Value;

    public static implicit operator string(Barcode barcode) => barcode.Value;
}
