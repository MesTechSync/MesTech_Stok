using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// ICredentialEncryptionService implementasyonu.
/// IFieldEncryptionService'i sarmalayarak credential-specific maskeleme ekler.
/// </summary>
public sealed class CredentialEncryptionService : ICredentialEncryptionService
{
    private readonly IFieldEncryptionService _fieldEncryption;
    private readonly ILogger<CredentialEncryptionService> _logger;

    public CredentialEncryptionService(
        IFieldEncryptionService fieldEncryption,
        ILogger<CredentialEncryptionService> logger)
    {
        _fieldEncryption = fieldEncryption;
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        return _fieldEncryption.Encrypt(plainText);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        return _fieldEncryption.Decrypt(cipherText);
    }

    /// <summary>
    /// Credential degerini maskeler.
    /// 8+ karakter: ilk 4 + **** + son 4
    /// 4-7 karakter: ilk 1 + **** + son 1
    /// 3 veya az: tamamen ****
    /// </summary>
    public string Mask(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return "****";

        if (plainText.Length <= 3)
            return "****";

        if (plainText.Length <= 7)
            return $"{plainText[0]}****{plainText[^1]}";

        return $"{plainText[..4]}****{plainText[^4..]}";
    }
}
