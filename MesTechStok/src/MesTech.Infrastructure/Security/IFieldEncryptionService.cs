namespace MesTech.Infrastructure.Security;

/// <summary>
/// Alan bazli sifreleme servisi arayuzu.
/// BankAccount.IBAN, Counterparty.VKN gibi hassas alanlarin sifrelenmesi icin kullanilir.
/// </summary>
public interface IFieldEncryptionService
{
    /// <summary>
    /// Duzyaziyi sifreler ve Base64 encoded ciphertext dondurur.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Base64 encoded ciphertext'i cozer ve duzyazi dondurur.
    /// </summary>
    string Decrypt(string cipherText);
}
