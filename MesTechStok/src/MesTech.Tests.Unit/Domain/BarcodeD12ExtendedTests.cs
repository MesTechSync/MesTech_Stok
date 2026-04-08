using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// D12-16 — Barcode Value Object genişletilmiş testleri.
/// Check digit doğrulama, BarcodeFormat, IsTurkish, GTIN-14, Create factory.
/// Mevcut BarcodeValueObjectTests'e DOKUNMAZ — ayrı dosya.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Barcode")]
public class BarcodeD12ExtendedTests
{
    // ══════════════════════════════════════
    // Barcode.Create() — check digit doğrulama
    // ══════════════════════════════════════

    [Theory]
    [InlineData("8690001000011")]    // TR EAN-13 geçerli
    [InlineData("8690000000005")]    // TR EAN-13 geçerli
    [InlineData("4006381333931")]    // DE EAN-13 geçerli (Henkel)
    public void Create_ValidEAN13_ShouldSucceed(string barcode)
    {
        var bc = Barcode.Create(barcode);
        bc.Format.Should().Be(BarcodeFormat.EAN13);
        bc.IsEAN13.Should().BeTrue();
    }

    [Theory]
    [InlineData("8690001000012")]    // Yanlış check digit (11 olmalı, 12 verilmiş)
    [InlineData("8690001000019")]    // Yanlış
    public void Create_InvalidCheckDigit_ShouldThrow(string barcode)
    {
        var act = () => Barcode.Create(barcode);
        act.Should().Throw<ArgumentException>().WithMessage("*kontrol basamagi*");
    }

    [Fact]
    public void Create_NullValue_ShouldThrow()
    {
        var act = () => Barcode.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceValue_ShouldThrow()
    {
        var act = () => Barcode.Create("   ");
        act.Should().Throw<ArgumentException>();
    }

    // ══════════════════════════════════════
    // BarcodeFormat detection
    // ══════════════════════════════════════

    [Fact]
    public void Format_8Digit_ShouldBeEAN8()
    {
        var bc = new Barcode("96385074");
        bc.Format.Should().Be(BarcodeFormat.EAN8);
        bc.IsEAN8.Should().BeTrue();
    }

    [Fact]
    public void Format_12Digit_ShouldBeUPCA()
    {
        var bc = new Barcode("012345678905");
        bc.Format.Should().Be(BarcodeFormat.UPCA);
        bc.IsUPC.Should().BeTrue();
    }

    [Fact]
    public void Format_13Digit_ShouldBeEAN13()
    {
        var bc = new Barcode("8690000000005");
        bc.Format.Should().Be(BarcodeFormat.EAN13);
    }

    [Fact]
    public void Format_14Digit_ShouldBeGTIN14()
    {
        var bc = new Barcode("07622210713681");
        bc.Format.Should().Be(BarcodeFormat.GTIN14);
    }

    [Fact]
    public void Format_NonNumeric_ShouldBeInternal()
    {
        var bc = new Barcode("INTERNAL-001");
        bc.Format.Should().Be(BarcodeFormat.Internal);
        bc.IsGS1.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // IsTurkish (868/869 prefix)
    // ══════════════════════════════════════

    [Theory]
    [InlineData("8690000000005", true)]   // 869 prefix → Türk
    [InlineData("8680000000013", true)]   // 868 prefix → Türk
    [InlineData("4006381333931", false)]  // 400 prefix → Alman
    [InlineData("INTERNAL-001", false)]   // dahili
    public void IsTurkish_ShouldDetectPrefix(string barcode, bool expected)
    {
        var bc = new Barcode(barcode);
        bc.IsTurkish.Should().Be(expected);
    }

    // ══════════════════════════════════════
    // ValidateCheckDigit static method
    // ══════════════════════════════════════

    [Theory]
    [InlineData("8690000000005", true)]
    [InlineData("8690000000001", false)]
    [InlineData("96385074", true)]       // EAN-8
    [InlineData("012345678905", true)]   // UPC-A
    [InlineData("ABC", false)]           // non-digit
    [InlineData("12345", false)]         // too short (< 8)
    public void ValidateCheckDigit_Various(string barcode, bool expected)
    {
        Barcode.ValidateCheckDigit(barcode).Should().Be(expected);
    }

    [Fact]
    public void ValidateCheckDigit_AllZeros_ShouldCalculate()
    {
        // 0000000000000 → check digit'i hesapla
        Barcode.ValidateCheckDigit("0000000000000").Should().BeTrue();
    }

    // ══════════════════════════════════════
    // CreateInternal factory
    // ══════════════════════════════════════

    [Fact]
    public void CreateInternal_ShouldForceInternalFormat()
    {
        var bc = Barcode.CreateInternal("ANY-SKU-CODE");
        bc.Format.Should().Be(BarcodeFormat.Internal);
        bc.Value.Should().Be("ANY-SKU-CODE");
    }

    // ══════════════════════════════════════
    // Hyphen/space cleaning
    // ══════════════════════════════════════

    [Fact]
    public void Create_WithHyphensAndSpaces_ShouldClean()
    {
        var bc = new Barcode("869-0000-0000-05");
        bc.Value.Should().Be("8690000000005");
        bc.IsEAN13.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // Unique barcodes stress test
    // ══════════════════════════════════════

    [Fact]
    public void Create400Barcodes_AllUnique_NoConflict()
    {
        var barcodes = new HashSet<string>();
        for (int i = 0; i < 400; i++)
        {
            var code = $"INTERNAL-{i:D4}";
            var bc = Barcode.CreateInternal(code);
            barcodes.Add(bc.Value).Should().BeTrue($"barcode {code} should be unique");
        }
        barcodes.Should().HaveCount(400);
    }
}
