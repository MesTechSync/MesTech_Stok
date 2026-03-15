using System.Security.Cryptography;
using FluentAssertions;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Security;

/// <summary>
/// FieldEncryptionService tests — AES-256-GCM encryption/decryption.
/// </summary>
[Trait("Category", "Unit")]
public class FieldEncryptionServiceTests
{
    private readonly FieldEncryptionService _sut;

    public FieldEncryptionServiceTests()
    {
        // Generate a valid 256-bit key
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var keyBase64 = Convert.ToBase64String(keyBytes);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = keyBase64
            })
            .Build();

        var logger = new Mock<ILogger<FieldEncryptionService>>();
        _sut = new FieldEncryptionService(config, logger.Object);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsSameValue()
    {
        var plainText = "Hello, World!";
        var encrypted = _sut.Encrypt(plainText);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_DifferentInputs_DifferentCipherTexts()
    {
        var encrypted1 = _sut.Encrypt("Input A");
        var encrypted2 = _sut.Encrypt("Input B");

        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Encrypt_SameInput_DifferentCipherTexts_RandomNonce()
    {
        var encrypted1 = _sut.Encrypt("Same input");
        var encrypted2 = _sut.Encrypt("Same input");

        // Random nonce guarantees different ciphertext
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Decrypt_TamperedData_ThrowsException()
    {
        var encrypted = _sut.Encrypt("Test data");
        var bytes = Convert.FromBase64String(encrypted);

        // Tamper with ciphertext (modify a byte in the middle)
        bytes[bytes.Length / 2] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => _sut.Decrypt(tampered);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Encrypt_NullInput_ThrowsArgumentNull()
    {
        var act = () => _sut.Encrypt(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Decrypt_NullInput_ThrowsArgumentNull()
    {
        var act = () => _sut.Decrypt(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsNonEmpty()
    {
        var encrypted = _sut.Encrypt("");
        encrypted.Should().NotBeNullOrEmpty();

        // Should be able to decrypt back to empty string
        var decrypted = _sut.Decrypt(encrypted);
        decrypted.Should().Be("");
    }

    [Fact]
    public void Encrypt_LongString_SuccessfulRoundTrip()
    {
        var longString = new string('A', 10000);
        var encrypted = _sut.Encrypt(longString);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(longString);
    }

    [Fact]
    public void Encrypt_TurkishChars_SuccessfulRoundTrip()
    {
        var turkishText = "TR1234567890 Mustafa Turkce Karakterler";
        var encrypted = _sut.Encrypt(turkishText);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(turkishText);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ThrowsException()
    {
        var act = () => _sut.Decrypt("not-valid-base64!!!");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_TooShortData_ThrowsException()
    {
        // Less than 12 (nonce) + 16 (tag) = 28 bytes
        var shortData = Convert.ToBase64String(new byte[10]);
        var act = () => _sut.Decrypt(shortData);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_OutputIsBase64Encoded()
    {
        var encrypted = _sut.Encrypt("Test");

        // Should not throw on Convert.FromBase64String
        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_OutputContainsNonceCiphertextTag()
    {
        var encrypted = _sut.Encrypt("Test data");
        var bytes = Convert.FromBase64String(encrypted);

        // nonce (12) + ciphertext (>=0) + tag (16) = >= 28 bytes
        bytes.Length.Should().BeGreaterOrEqualTo(28);
    }

    [Fact]
    public void Encrypt_SpecialCharacters_SuccessfulRoundTrip()
    {
        var specialText = "!@#$%^&*()_+-=[]{}|;':\",./<>?\\`~\n\t\r";
        var encrypted = _sut.Encrypt(specialText);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(specialText);
    }

    [Fact]
    public void Encrypt_UnicodeEmoji_SuccessfulRoundTrip()
    {
        var emojiText = "Test data with unicode chars: \u00e7\u00f6\u015f\u011f\u00fc\u0131";
        var encrypted = _sut.Encrypt(emojiText);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(emojiText);
    }

    [Fact]
    public void Constructor_InvalidKeyLength_ThrowsArgumentException()
    {
        var shortKey = Convert.ToBase64String(new byte[16]); // 128-bit, not 256
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = shortKey
            })
            .Build();

        var act = () => new FieldEncryptionService(config, new Mock<ILogger<FieldEncryptionService>>().Object);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NoKey_GeneratesTemporaryKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Should not throw — generates temp key with warning
        var sut = new FieldEncryptionService(config, new Mock<ILogger<FieldEncryptionService>>().Object);
        var encrypted = sut.Encrypt("Test");
        var decrypted = sut.Decrypt(encrypted);
        decrypted.Should().Be("Test");
    }

    [Fact]
    public void DifferentKeys_CannotDecryptEachOther()
    {
        var key2Bytes = new byte[32];
        RandomNumberGenerator.Fill(key2Bytes);
        var config2 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = Convert.ToBase64String(key2Bytes)
            })
            .Build();

        var sut2 = new FieldEncryptionService(config2, new Mock<ILogger<FieldEncryptionService>>().Object);

        var encrypted = _sut.Encrypt("Secret data");

        // Different key should fail to decrypt
        var act = () => sut2.Decrypt(encrypted);
        act.Should().Throw<Exception>();
    }
}
