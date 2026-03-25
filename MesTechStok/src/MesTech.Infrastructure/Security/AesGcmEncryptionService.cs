using System.Security.Cryptography;
using System.Text;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// AES-256-GCM ile credential şifreleme servisi.
/// StoreCredential değerlerini şifreler/çözer.
/// </summary>
public sealed class AesGcmEncryptionService
{
    private readonly byte[] _key;

    public AesGcmEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new ArgumentException("Key must be 256 bits (32 bytes).");
    }

    public string Encrypt(string plainText)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // nonce + tag + cipher -> base64
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        cipherBytes.CopyTo(result, nonce.Length + tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        var data = Convert.FromBase64String(encryptedText);

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = data[..nonceSize];
        var tag = data[nonceSize..(nonceSize + tagSize)];
        var cipherBytes = data[(nonceSize + tagSize)..];

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Yeni rastgele 256-bit key üretir (ilk kurulumda kullanılır).
    /// </summary>
    public static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
