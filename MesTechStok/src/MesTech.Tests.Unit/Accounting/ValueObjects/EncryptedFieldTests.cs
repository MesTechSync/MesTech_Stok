using FluentAssertions;
using MesTech.Domain.Accounting.ValueObjects;

namespace MesTech.Tests.Unit.Accounting.ValueObjects;

[Trait("Category", "Unit")]
public class EncryptedFieldTests
{
    [Fact]
    public void Create_WithCipherText_ShouldStoreCipherText()
    {
        var field = new EncryptedField("encrypted_data_here");

        field.CipherText.Should().Be("encrypted_data_here");
    }

    [Fact]
    public void Create_WithNull_ShouldStoreEmpty()
    {
        var field = new EncryptedField(null!);

        field.CipherText.Should().Be(string.Empty);
    }

    [Fact]
    public void Create_WithEmpty_ShouldStoreEmpty()
    {
        var field = new EncryptedField(string.Empty);

        field.CipherText.Should().Be(string.Empty);
    }

    [Fact]
    public void ToString_ShouldReturnMaskedValue()
    {
        var field = new EncryptedField("1234567890");

        var result = field.ToString();

        result.Should().EndWith("7890");
        result.Should().StartWith("*");
        result.Length.Should().Be(10);
    }

    [Fact]
    public void ToString_ShortCipherText_ShouldReturnStars()
    {
        var field = new EncryptedField("abc");

        field.ToString().Should().Be("****");
    }

    [Fact]
    public void ToString_EmptyCipherText_ShouldReturnStars()
    {
        var field = new EncryptedField("");

        field.ToString().Should().Be("****");
    }

    [Fact]
    public void ToString_ExactlyFourChars_ShouldReturnStars()
    {
        var field = new EncryptedField("abcd");

        field.ToString().Should().Be("****");
    }

    [Fact]
    public void ToString_FiveChars_ShouldMaskFirst()
    {
        var field = new EncryptedField("abcde");

        field.ToString().Should().Be("*bcde");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnCipherText()
    {
        var field = new EncryptedField("encrypted_value");
        string str = field;

        str.Should().Be("encrypted_value");
    }

    [Fact]
    public void FromPlainText_ShouldCreateField()
    {
        var field = EncryptedField.FromPlainText("my_secret");

        // Placeholder implementation passes through plain text
        field.CipherText.Should().Be("my_secret");
    }

    [Fact]
    public void Equality_SameCipherText_ShouldBeEqual()
    {
        var field1 = new EncryptedField("same_value");
        var field2 = new EncryptedField("same_value");

        field1.Should().Be(field2);
    }

    [Fact]
    public void Equality_DifferentCipherText_ShouldNotBeEqual()
    {
        var field1 = new EncryptedField("value_a");
        var field2 = new EncryptedField("value_b");

        field1.Should().NotBe(field2);
    }

    [Fact]
    public void GetHashCode_SameValue_ShouldBeSame()
    {
        var field1 = new EncryptedField("same");
        var field2 = new EncryptedField("same");

        field1.GetHashCode().Should().Be(field2.GetHashCode());
    }
}
