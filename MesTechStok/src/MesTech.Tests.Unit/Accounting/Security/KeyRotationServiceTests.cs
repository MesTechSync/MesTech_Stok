using System.Security.Cryptography;
using FluentAssertions;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Security;

/// <summary>
/// KeyRotationService tests — re-encryption from old key to new key.
/// </summary>
[Trait("Category", "Unit")]
public class KeyRotationServiceTests
{
    private static string GenerateBase64Key()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static (FieldEncryptionService oldEncryption, KeyRotationService sut, IFieldEncryptionService newEncryption) CreateRotationSetup()
    {
        var oldKey = GenerateBase64Key();
        var newKey = GenerateBase64Key();

        var oldConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = oldKey
            })
            .Build();

        var newConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = newKey,
                ["Encryption:PreviousKey"] = oldKey
            })
            .Build();

        var loggerField = new Mock<ILogger<FieldEncryptionService>>();
        var loggerRotation = new Mock<ILogger<KeyRotationService>>();

        var oldEncryption = new FieldEncryptionService(oldConfig, loggerField.Object);
        var newEncryption = new FieldEncryptionService(newConfig, loggerField.Object);

        var sut = new KeyRotationService(newEncryption, newConfig, loggerRotation.Object);

        return (oldEncryption, sut, newEncryption);
    }

    [Fact]
    public void RotateKey_ReEncryptsWithNewKey()
    {
        var (oldEncryption, sut, newEncryption) = CreateRotationSetup();

        var plainText = "Sensitive IBAN data";
        var encryptedWithOldKey = oldEncryption.Encrypt(plainText);

        // Rotate should decrypt with old key and re-encrypt with new key
        var rotated = sut.Rotate(encryptedWithOldKey);

        // Rotated should be different from original
        rotated.Should().NotBe(encryptedWithOldKey);

        // Should be decryptable with new key
        var decrypted = newEncryption.Decrypt(rotated);
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void RotateKey_AlreadyNewKey_ReturnsOriginal()
    {
        var (_, sut, newEncryption) = CreateRotationSetup();

        var plainText = "Already new key data";
        var encryptedWithNewKey = newEncryption.Encrypt(plainText);

        // If already encrypted with new key, old key decryption will fail,
        // and the service returns the original ciphertext
        var rotated = sut.Rotate(encryptedWithNewKey);

        // Should be decryptable with new key (either same or re-encrypted)
        var decrypted = newEncryption.Decrypt(rotated);
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void IsRotationNeeded_WithPreviousKey_ReturnsTrue()
    {
        var (_, sut, _) = CreateRotationSetup();
        sut.IsRotationNeeded.Should().BeTrue();
    }

    [Fact]
    public void IsRotationNeeded_NoPreviousKey_ReturnsFalse()
    {
        var newKey = GenerateBase64Key();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = newKey
            })
            .Build();

        var encryption = new FieldEncryptionService(config, new Mock<ILogger<FieldEncryptionService>>().Object);
        var sut = new KeyRotationService(encryption, config, new Mock<ILogger<KeyRotationService>>().Object);

        sut.IsRotationNeeded.Should().BeFalse();
    }

    [Fact]
    public void Rotate_WhenNotNeeded_ReturnsOriginalText()
    {
        var key = GenerateBase64Key();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = key
            })
            .Build();

        var encryption = new FieldEncryptionService(config, new Mock<ILogger<FieldEncryptionService>>().Object);
        var sut = new KeyRotationService(encryption, config, new Mock<ILogger<KeyRotationService>>().Object);

        var encrypted = encryption.Encrypt("Data");
        var rotated = sut.Rotate(encrypted);

        rotated.Should().Be(encrypted);
    }

    [Fact]
    public void IsRotationNeeded_SameKeyInBothFields_ReturnsFalse()
    {
        var sameKey = GenerateBase64Key();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = sameKey,
                ["Encryption:PreviousKey"] = sameKey
            })
            .Build();

        var encryption = new FieldEncryptionService(config, new Mock<ILogger<FieldEncryptionService>>().Object);
        var sut = new KeyRotationService(encryption, config, new Mock<ILogger<KeyRotationService>>().Object);

        sut.IsRotationNeeded.Should().BeFalse();
    }

    [Fact]
    public void RotateKey_MultipleFields_AllDecryptableWithNewKey()
    {
        var (oldEncryption, sut, newEncryption) = CreateRotationSetup();

        var fields = new[] { "TR123456789", "1234567890", "Secret VKN Data" };
        var encrypted = fields.Select(f => oldEncryption.Encrypt(f)).ToArray();
        var rotated = encrypted.Select(e => sut.Rotate(e)).ToArray();

        for (int i = 0; i < fields.Length; i++)
        {
            var decrypted = newEncryption.Decrypt(rotated[i]);
            decrypted.Should().Be(fields[i]);
        }
    }
}
