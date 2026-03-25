using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Sifreleme anahtari rotasyon servisi arayuzu.
/// Eski anahtar ile cozup yeni anahtar ile sifreler.
/// </summary>
public interface IKeyRotationService
{
    /// <summary>
    /// Sifrelenmis metni eski anahtarla cozup yeni anahtarla yeniden sifreler.
    /// Eger metin eski anahtarla cozulemezse (zaten yeni anahtarla sifrelenmis),
    /// mevcut metni oldugu gibi dondurur.
    /// </summary>
    string Rotate(string encryptedText);

    /// <summary>
    /// Rotasyon gerekli mi — PreviousKey yapilandirilmis ve farkli mi?
    /// </summary>
    bool IsRotationNeeded { get; }
}

/// <summary>
/// AES-256-GCM anahtar rotasyon implementasyonu.
/// Config: Encryption:Key (yeni), Encryption:PreviousKey (eski — rotasyon icin).
/// </summary>
public sealed class KeyRotationService : IKeyRotationService
{
    private readonly IFieldEncryptionService _currentEncryption;
    private readonly FieldEncryptionService? _previousEncryption;
    private readonly ILogger<KeyRotationService> _logger;

    public bool IsRotationNeeded { get; }

    public KeyRotationService(
        IFieldEncryptionService currentEncryption,
        IConfiguration configuration,
        ILogger<KeyRotationService> logger)
    {
        _currentEncryption = currentEncryption;
        _logger = logger;

        var previousKey = configuration["Encryption:PreviousKey"];
        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            var currentKey = configuration["Encryption:Key"];
            if (previousKey != currentKey)
            {
                // Eski key icin ayri bir FieldEncryptionService olustur
                var previousConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Encryption:Key"] = previousKey
                    })
                    .Build();

                _previousEncryption = new FieldEncryptionService(
                    previousConfig,
                    new LoggerFactory().CreateLogger<FieldEncryptionService>());
                IsRotationNeeded = true;

                _logger.LogInformation(
                    "[KeyRotation] PreviousKey yapilandirilmis — anahtar rotasyonu aktif");
            }
        }
    }

    public string Rotate(string encryptedText)
    {
        if (!IsRotationNeeded || _previousEncryption == null)
        {
            return encryptedText;
        }

        try
        {
            // Eski anahtar ile coz
            var plainText = _previousEncryption.Decrypt(encryptedText);

            // Yeni anahtar ile sifrele
            var reEncrypted = _currentEncryption.Encrypt(plainText);

            _logger.LogDebug("[KeyRotation] Alan basariyla yeni anahtara rotate edildi");
            return reEncrypted;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Eski anahtarla cozulemediyse, zaten yeni anahtarla sifrelenmis olabilir
            _logger.LogDebug(
                "[KeyRotation] Eski anahtarla cozulemedi — muhtemelen zaten yeni anahtar ile sifrelenmis");
            return encryptedText;
        }
    }
}
