using MesTech.Infrastructure.Security;

namespace MesTech.Infrastructure.Integration.Security;

/// <summary>
/// Feed HTTP Basic Auth credential'larını şifreli saklayan servis.
/// AES-256-GCM kullanır — repoda asla plain-text gözükmez.
/// ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-07
/// </summary>
public interface IFeedCredentialProtector
{
    /// <summary>
    /// Kullanıcı adı ve şifreyi şifreli tek bir string'e dönüştürür.
    /// Dönen değer SupplierFeed._encryptedCredential alanına kaydedilir.
    /// </summary>
    string Protect(string username, string password);

    /// <summary>
    /// Şifrelenmiş değeri çözer ve kullanıcı adı + şifreyi döner.
    /// </summary>
    (string Username, string Password) Unprotect(string protectedValue);
}

/// <summary>
/// <see cref="IFeedCredentialProtector"/> implementasyonu.
/// Dahili şifreleme için <see cref="AesGcmEncryptionService"/> kullanır.
/// Separator olarak ASCII Unit Separator (0x1F) kullanılır — URL/JSON safe.
/// </summary>
public sealed class FeedCredentialProtector : IFeedCredentialProtector
{
    private const char Separator = '\u001F'; // ASCII Unit Separator

    private readonly AesGcmEncryptionService _encryption;

    /// <summary>
    /// <paramref name="encryptionKey"/> yoksa ya da geçersizse
    /// uygulama çalışma zamanında rastgele bir anahtar üretir (development).
    /// Production'da <c>FeedCredentials:EncryptionKey</c> config değeri set edilmeli.
    /// </summary>
    public FeedCredentialProtector(string? encryptionKey = null)
    {
        var key = string.IsNullOrWhiteSpace(encryptionKey)
            ? AesGcmEncryptionService.GenerateKey()
            : encryptionKey;

        _encryption = new AesGcmEncryptionService(key);
    }

    /// <inheritdoc/>
    public string Protect(string username, string password)
    {
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            throw new ArgumentException(
                "Kullanıcı adı veya şifre gerekli.", nameof(username));

        var combined = $"{username}{Separator}{password}";
        return _encryption.Encrypt(combined);
    }

    /// <inheritdoc/>
    public (string Username, string Password) Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
            throw new ArgumentException(
                "Şifrelenmiş değer boş olamaz.", nameof(protectedValue));

        var combined = _encryption.Decrypt(protectedValue);
        var parts    = combined.Split(Separator, 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (combined, string.Empty);
    }
}
