using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Webhooks.Validators;

namespace MesTech.Tests.Unit.Infrastructure.Webhooks;

/// <summary>
/// HH-DEV5-017: Webhook signature validation tests.
/// Tests HMAC-based signature verification for multiple platforms.
/// Ensures timing-safe comparison and rejects invalid/empty inputs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "WebhookSignature")]
[Trait("Phase", "Dalga15")]
public class WebhookSignatureValidatorTests
{
    private const string TestSecret = "whsec_test_secret_key_2026";
    private const string TestBody = @"{""orderId"":""ORD-001"",""status"":""shipped""}";

    // ═══════════════════════════════════════════
    // Trendyol — SHA256 HMAC hex
    // ═══════════════════════════════════════════

    [Fact]
    public void Trendyol_Platform_ShouldBe_trendyol()
    {
        var sut = new TrendyolSignatureValidator();
        sut.Platform.Should().Be("trendyol");
    }

    [Fact]
    public void Trendyol_ValidSignature_ReturnsTrue()
    {
        var sut = new TrendyolSignatureValidator();
        var expectedSig = ComputeHmacSha256Hex(TestBody, TestSecret);

        sut.Validate(TestBody, expectedSig, TestSecret).Should().BeTrue();
    }

    [Fact]
    public void Trendyol_InvalidSignature_ReturnsFalse()
    {
        var sut = new TrendyolSignatureValidator();

        sut.Validate(TestBody, "invalid-hex-signature", TestSecret).Should().BeFalse();
    }

    [Fact]
    public void Trendyol_WrongSecret_ReturnsFalse()
    {
        var sut = new TrendyolSignatureValidator();
        var sigWithCorrectSecret = ComputeHmacSha256Hex(TestBody, TestSecret);

        sut.Validate(TestBody, sigWithCorrectSecret, "wrong_secret").Should().BeFalse();
    }

    [Fact]
    public void Trendyol_TamperedBody_ReturnsFalse()
    {
        var sut = new TrendyolSignatureValidator();
        var sigForOriginal = ComputeHmacSha256Hex(TestBody, TestSecret);
        var tamperedBody = TestBody.Replace("ORD-001", "ORD-TAMPERED");

        sut.Validate(tamperedBody, sigForOriginal, TestSecret).Should().BeFalse();
    }

    [Theory]
    [InlineData("", TestSecret)]
    [InlineData(null, TestSecret)]
    public void Trendyol_EmptyOrNullSignature_ReturnsFalse(string? signature, string secret)
    {
        var sut = new TrendyolSignatureValidator();
        sut.Validate(TestBody, signature!, secret).Should().BeFalse();
    }

    [Theory]
    [InlineData("some-sig", "")]
    [InlineData("some-sig", null)]
    public void Trendyol_EmptyOrNullSecret_ReturnsFalse(string signature, string? secret)
    {
        var sut = new TrendyolSignatureValidator();
        sut.Validate(TestBody, signature, secret!).Should().BeFalse();
    }

    [Fact]
    public void Trendyol_CaseInsensitiveSignature_ReturnsTrue()
    {
        var sut = new TrendyolSignatureValidator();
        var sigLower = ComputeHmacSha256Hex(TestBody, TestSecret).ToLowerInvariant();
        var sigUpper = sigLower.ToUpperInvariant();

        // Both cases should be valid (hex comparison is case-insensitive)
        sut.Validate(TestBody, sigLower, TestSecret).Should().BeTrue();
        sut.Validate(TestBody, sigUpper, TestSecret).Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // Shopify — SHA256 HMAC Base64
    // ═══════════════════════════════════════════

    [Fact]
    public void Shopify_Platform_ShouldBe_shopify()
    {
        var sut = new ShopifySignatureValidator();
        sut.Platform.Should().Be("shopify");
    }

    [Fact]
    public void Shopify_ValidSignature_ReturnsTrue()
    {
        var sut = new ShopifySignatureValidator();
        var expectedSig = ComputeHmacSha256Base64(TestBody, TestSecret);

        sut.Validate(TestBody, expectedSig, TestSecret).Should().BeTrue();
    }

    [Fact]
    public void Shopify_InvalidSignature_ReturnsFalse()
    {
        var sut = new ShopifySignatureValidator();

        sut.Validate(TestBody, "aW52YWxpZA==", TestSecret).Should().BeFalse();
    }

    [Fact]
    public void Shopify_TamperedBody_ReturnsFalse()
    {
        var sut = new ShopifySignatureValidator();
        var sigForOriginal = ComputeHmacSha256Base64(TestBody, TestSecret);
        var tamperedBody = TestBody.Replace("shipped", "cancelled");

        sut.Validate(tamperedBody, sigForOriginal, TestSecret).Should().BeFalse();
    }

    [Fact]
    public void Shopify_EmptySignature_ReturnsFalse()
    {
        var sut = new ShopifySignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // Hepsiburada
    // ═══════════════════════════════════════════

    [Fact]
    public void Hepsiburada_Platform_ShouldBe_hepsiburada()
    {
        var sut = new HepsiburadaSignatureValidator();
        sut.Platform.Should().Be("hepsiburada");
    }

    [Fact]
    public void Hepsiburada_EmptySignature_ReturnsFalse()
    {
        var sut = new HepsiburadaSignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // N11
    // ═══════════════════════════════════════════

    [Fact]
    public void N11_Platform_ShouldBe_n11()
    {
        var sut = new N11SignatureValidator();
        sut.Platform.Should().Be("n11");
    }

    [Fact]
    public void N11_EmptySignature_ReturnsFalse()
    {
        var sut = new N11SignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // CicekSepeti
    // ═══════════════════════════════════════════

    [Fact]
    public void CicekSepeti_Platform_ShouldBe_ciceksepeti()
    {
        var sut = new CiceksepetiSignatureValidator();
        sut.Platform.Should().Be("ciceksepeti");
    }

    [Fact]
    public void CicekSepeti_EmptySignature_ReturnsFalse()
    {
        var sut = new CiceksepetiSignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // WooCommerce
    // ═══════════════════════════════════════════

    [Fact]
    public void WooCommerce_Platform_ShouldBe_woocommerce()
    {
        var sut = new WooCommerceSignatureValidator();
        sut.Platform.Should().Be("woocommerce");
    }

    [Fact]
    public void WooCommerce_EmptySignature_ReturnsFalse()
    {
        var sut = new WooCommerceSignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // eBay
    // ═══════════════════════════════════════════

    [Fact]
    public void Ebay_Platform_ShouldBe_ebay()
    {
        var sut = new EbaySignatureValidator();
        sut.Platform.Should().Be("ebay");
    }

    [Fact]
    public void Ebay_EmptySignature_ReturnsFalse()
    {
        var sut = new EbaySignatureValidator();
        sut.Validate(TestBody, "", TestSecret).Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════

    private static string ComputeHmacSha256Hex(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHmacSha256Base64(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        return Convert.ToBase64String(hash);
    }
}
