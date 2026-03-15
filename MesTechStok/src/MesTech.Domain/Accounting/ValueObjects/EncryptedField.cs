namespace MesTech.Domain.Accounting.ValueObjects;

/// <summary>
/// Sifreli alan value object — CipherText saklar, ToString() maskelenmis deger dondurur.
/// Sifreleme/desifreleme Infrastructure katmaninda yapilir.
/// </summary>
public sealed record EncryptedField
{
    public string CipherText { get; }

    public EncryptedField(string cipherText)
    {
        CipherText = cipherText ?? string.Empty;
    }

    /// <summary>
    /// Maskelenmis deger — son 4 karakter gorunur, geri kalani yildiz.
    /// </summary>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(CipherText) || CipherText.Length <= 4)
            return "****";

        return new string('*', CipherText.Length - 4) + CipherText[^4..];
    }

    public static implicit operator string(EncryptedField field) => field.CipherText;

    public static EncryptedField FromPlainText(string plainText)
    {
        // Placeholder: Infrastructure katmaninda gercek sifreleme yapilir.
        return new EncryptedField(plainText);
    }
}
