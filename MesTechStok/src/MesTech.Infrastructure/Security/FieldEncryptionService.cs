using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// AES-256-GCM tabanli alan sifreleme servisi.
/// BankAccount.IBAN, Counterparty.VKN gibi hassas alanlarin DB'de sifrelenmesi icin.
///
/// Sifreli format: Base64(nonce[12] + ciphertext[N] + tag[16])
/// Key: IConfiguration["Encryption:Key"] veya user-secrets/env var.
/// </summary>
public class FieldEncryptionService : IFieldEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<FieldEncryptionService> _logger;

    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public FieldEncryptionService(IConfiguration configuration, ILogger<FieldEncryptionService> logger)
    {
        _logger = logger;

        var keyString = configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyString))
        {
            _logger.LogWarning(
                "[FieldEncryption] Encryption:Key yapilandirilmamis, " +
                "gecici key uretiliyor — PRODUCTION'da env var/user-secrets kullanin!");
            keyString = AesGcmEncryptionService.GenerateKey();
        }

        _key = Convert.FromBase64String(keyString);
        if (_key.Length != 32)
        {
            throw new ArgumentException(
                "Encryption key 256-bit (32 byte) olmali. " +
                "Mevcut uzunluk: " + _key.Length + " byte.");
        }
    }

    /// <summary>
    /// Duzyaziyi AES-256-GCM ile sifreler.
    /// Cikti: Base64(nonce[12] + ciphertext[N] + tag[16])
    /// </summary>
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // nonce + ciphertext + tag -> Base64
        var result = new byte[NonceSizeBytes + cipherBytes.Length + TagSizeBytes];
        nonce.CopyTo(result, 0);
        cipherBytes.CopyTo(result, NonceSizeBytes);
        tag.CopyTo(result, NonceSizeBytes + cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// AES-256-GCM sifreli metni cozer.
    /// Girdi: Base64(nonce[12] + ciphertext[N] + tag[16])
    /// </summary>
    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var data = Convert.FromBase64String(cipherText);

        if (data.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new ArgumentException("Sifreli veri cok kisa — bozuk veya hatali format.");
        }

        var nonce = data[..NonceSizeBytes];
        var cipherBytes = data[NonceSizeBytes..^TagSizeBytes];
        var tag = data[^TagSizeBytes..];

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
