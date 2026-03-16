namespace MesTech.Application.Interfaces;

/// <summary>
/// Credential degerlerini sifreler/cozer.
/// AES-256-GCM tabanli — StoreCredential.EncryptedValue icin kullanilir.
/// </summary>
public interface ICredentialEncryptionService
{
    /// <summary>
    /// Duzyazi credential degerini sifreler.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Sifrelenmis credential degerini cozer.
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Credential degerini maskeler: ilk 4 + son 4 karakter gorunur, aradakiler "*".
    /// 8 veya daha kisa degerler icin tamamen maskelenir.
    /// </summary>
    string Mask(string plainText);
}
