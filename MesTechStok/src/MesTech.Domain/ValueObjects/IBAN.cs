using System.Numerics;

namespace MesTech.Domain.ValueObjects;

/// <summary>
/// IBAN value object — TR + 24 rakam dogrulama (ISO 13616).
/// Immutable, self-validating.
/// </summary>
public record IBAN
{
    public string Value { get; }

    public IBAN(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        if (normalized.Length < 15 || normalized.Length > 34)
            throw new ArgumentException("IBAN uzunlugu 15-34 karakter olmali.", nameof(value));

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z]{2}\d{2}[A-Z0-9]+$"))
            throw new ArgumentException("IBAN formati gecersiz.", nameof(value));

        if (!ValidateChecksum(normalized))
            throw new ArgumentException("IBAN checksum dogrulamasi basarisiz.", nameof(value));

        Value = normalized;
    }

    /// <summary>TR IBAN olustur (TR + 24 rakam = 26 karakter).</summary>
    public bool IsTurkish => Value.StartsWith("TR") && Value.Length == 26;

    /// <summary>Banka kodu (TR IBAN icin 5 haneli).</summary>
    public string? BankCode => IsTurkish ? Value.Substring(4, 5) : null;

    /// <summary>Formatli gosterim: TR00 0000 0000 0000 0000 0000 00</summary>
    public string Formatted
    {
        get
        {
            var parts = new List<string>();
            for (int i = 0; i < Value.Length; i += 4)
                parts.Add(Value.Substring(i, Math.Min(4, Value.Length - i)));
            return string.Join(" ", parts);
        }
    }

    /// <summary>ISO 13616 Mod-97 checksum dogrulama.</summary>
    private static bool ValidateChecksum(string iban)
    {
        // Ilk 4 karakteri sona tasi
        var rearranged = iban[4..] + iban[..4];

        // Harfleri sayiya cevir (A=10, B=11, ..., Z=35)
        var numeric = string.Concat(rearranged.Select(c =>
            char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        // Mod 97 hesapla
        var number = BigInteger.Parse(numeric);
        return number % 97 == 1;
    }

    public override string ToString() => Value;

    // Implicit conversions
    public static implicit operator string(IBAN iban) => iban.Value;
}
