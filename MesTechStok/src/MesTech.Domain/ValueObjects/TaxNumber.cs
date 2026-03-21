namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Vergi numarasi value object — 10 haneli (tuzel kisi) veya 11 haneli TC kimlik (gercek kisi).
/// Immutable, self-validating (Turkiye vergi mevzuati).
/// </summary>
public record TaxNumber
{
    public string Value { get; }

    /// <summary>Tuzel kisi (10 hane) mi yoksa gercek kisi (11 hane TC) mi?</summary>
    public bool IsIndividual => Value.Length == 11;

    /// <summary>Tuzel kisi vergi numarasi mi?</summary>
    public bool IsCorporate => Value.Length == 10;

    public TaxNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();

        if (!normalized.All(char.IsDigit))
            throw new ArgumentException("Vergi numarasi sadece rakamlardan olusmali.", nameof(value));

        if (normalized.Length == 10)
        {
            if (!ValidateTaxNumber10(normalized))
                throw new ArgumentException("10 haneli vergi numarasi checksum dogrulamasi basarisiz.", nameof(value));
        }
        else if (normalized.Length == 11)
        {
            if (!ValidateTCKimlik(normalized))
                throw new ArgumentException("11 haneli TC kimlik numarasi dogrulamasi basarisiz.", nameof(value));
        }
        else
        {
            throw new ArgumentException("Vergi numarasi 10 (tuzel) veya 11 (gercek) haneli olmali.", nameof(value));
        }

        Value = normalized;
    }

    /// <summary>10 haneli vergi numarasi dogrulama (GIB algoritmasi).</summary>
    private static bool ValidateTaxNumber10(string vkn)
    {
        var digits = vkn.Select(c => c - '0').ToArray();
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            int tmp = (digits[i] + (9 - i)) % 10;
            int val = (tmp * (1 << (9 - i))) % 9;
            if (tmp != 0 && val == 0) val = 9;
            sum += val;
        }
        return (10 - (sum % 10)) % 10 == digits[9];
    }

    /// <summary>11 haneli TC kimlik numarasi dogrulama.</summary>
    private static bool ValidateTCKimlik(string tc)
    {
        if (tc[0] == '0') return false;

        var digits = tc.Select(c => c - '0').ToArray();

        // 10. hane: (ilk 9 toplami) % 10
        int sum9 = digits.Take(9).Sum();
        if (sum9 % 10 != digits[9]) return false;

        // 11. hane: (ilk 10 toplami) % 10
        int sum10 = digits.Take(10).Sum();
        if (sum10 % 10 != digits[10]) return false;

        // Ek kontrol: tek/cift hane kurali
        int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        int evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        int check10 = ((oddSum * 7) - evenSum) % 10;
        if (check10 < 0) check10 += 10;
        return check10 == digits[9];
    }

    public override string ToString() => Value;

    public static implicit operator string(TaxNumber tn) => tn.Value;
}
