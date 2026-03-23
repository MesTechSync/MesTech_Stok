using System.Security.Cryptography;
using System.Text;

namespace MesTech.Domain.Accounting.ValueObjects;

/// <summary>
/// AES-256-GCM sifreli alan value object.
/// CipherText olarak saklar, ToString() maskelenmis deger dondurur.
///
/// Sifreleme:
///   var encrypted = EncryptedField.Encrypt("1234567890", key);
///   var plain = encrypted.Decrypt(key);  // "1234567890"
///
/// Depolama:
///   DB'de CipherText (string) olarak saklanir.
///   Format: Base64(nonce[12] + ciphertext[N] + tag[16])
///
/// Key:
///   32 byte (256 bit). IConfiguration["Encryption:Key"] → Base64 encoded.
///   Olusturma: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
/// </summary>
public sealed record EncryptedField
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string CipherText { get; }

    public EncryptedField(string cipherText)
    {
        CipherText = cipherText ?? string.Empty;
    }

    /// <summary>Maskelenmis deger — son 4 karakter gorunur.</summary>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(CipherText) || CipherText.Length <= 4)
            return "****";

        return new string('*', CipherText.Length - 4) + CipherText[^4..];
    }

    public static implicit operator string(EncryptedField field) => field.CipherText;

    /// <summary>Sifrelenmis mi kontrolu — AES-GCM cikti minimum 29 byte (nonce+tag+1).</summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(CipherText) && CipherText.Length > 40;

    /// <summary>
    /// Duz metni AES-256-GCM ile sifrele.
    /// Her cagri farkli nonce uretir — ayni metin farkli cikti verir.
    /// </summary>
    public static EncryptedField Encrypt(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText))
            return new EncryptedField(string.Empty);

        ValidateKey(key);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var combined = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, combined, NonceSize + cipherBytes.Length, TagSize);

        return new EncryptedField(Convert.ToBase64String(combined));
    }

    /// <summary>
    /// Sifreli veriyi coz. Yanlis key → CryptographicException.
    /// </summary>
    public string Decrypt(byte[] key)
    {
        if (string.IsNullOrEmpty(CipherText))
            return string.Empty;

        ValidateKey(key);

        var combined = Convert.FromBase64String(CipherText);

        if (combined.Length < NonceSize + TagSize + 1)
            throw new CryptographicException(
                $"Sifreli veri cok kisa ({combined.Length} byte). Minimum: {NonceSize + TagSize + 1}");

        var nonce = new byte[NonceSize];
        var cipherBytes = new byte[combined.Length - NonceSize - TagSize];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, cipherBytes, 0, cipherBytes.Length);
        Buffer.BlockCopy(combined, NonceSize + cipherBytes.Length, tag, 0, TagSize);

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Eski base64/duz metin veriyi AES-GCM'e migrate et.
    /// </summary>
    public static EncryptedField MigrateFromLegacy(string legacyValue, byte[] newKey)
    {
        if (string.IsNullOrEmpty(legacyValue))
            return new EncryptedField(string.Empty);

        return Encrypt(legacyValue, newKey);
    }

    /// <summary>Bos EncryptedField.</summary>
    public static EncryptedField Empty => new(string.Empty);

    /// <summary>
    /// Placeholder uyumluluk — eski FromPlainText cagranlari icin.
    /// Gercek sifreleme icin Encrypt(plainText, key) kullanin.
    /// </summary>
    public static EncryptedField FromPlainText(string plainText)
    {
        return new EncryptedField(plainText);
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != 32)
            throw new ArgumentException(
                $"Encryption key 32 byte (256 bit) olmali. Gelen: {key?.Length ?? 0} byte.", nameof(key));
    }
}
