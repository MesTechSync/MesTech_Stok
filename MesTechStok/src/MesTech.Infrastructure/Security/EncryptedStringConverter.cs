using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// EF Core Value Converter — hassas string alanlari DB'de sifrelenmis saklar.
/// IFieldEncryptionService (AES-256-GCM) uzerinden transparant encrypt/decrypt.
/// Kullanim: BankAccount.IBAN, Counterparty.VKN gibi KVKK kapsamindaki alanlar.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IFieldEncryptionService encryptionService)
        : base(
            v => v == null ? v! : encryptionService.Encrypt(v),
            v => v == null ? v! : encryptionService.Decrypt(v))
    { }
}

/// <summary>
/// Nullable string icin EF Core Value Converter — null degerler korunur.
/// </summary>
public sealed class NullableEncryptedStringConverter : ValueConverter<string?, string?>
{
    public NullableEncryptedStringConverter(IFieldEncryptionService encryptionService)
        : base(
            v => v == null ? null : encryptionService.Encrypt(v),
            v => v == null ? null : encryptionService.Decrypt(v))
    { }
}
