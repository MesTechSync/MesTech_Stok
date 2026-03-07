namespace MesTech.Domain.ValueObjects;

/// <summary>
/// SKU format kuralları içeren value object.
/// </summary>
public record SKU
{
    public string Value { get; }

    public SKU(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(SKU sku) => sku.Value;
}
