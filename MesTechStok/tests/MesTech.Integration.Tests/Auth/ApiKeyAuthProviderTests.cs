using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Xunit;

namespace MesTech.Integration.Tests.Auth;

[Trait("Category", "Unit")]
public class ApiKeyAuthProviderTests
{
    private static ApiKeyAuthProvider CreateProvider(
        string platformCode = "Trendyol",
        string apiKey = "test-api-key",
        string apiSecret = "test-api-secret")
        => new(platformCode, apiKey, apiSecret);

    [Fact]
    public async Task GetTokenAsync_ValidApiKeyAndSecret_ReturnsBase64EncodedToken()
    {
        // Arrange
        var provider = CreateProvider(apiKey: "my-api-key", apiSecret: "my-api-secret");
        var expectedToken = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("my-api-key:my-api-secret"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedToken);
        result.TokenType.Should().Be("Basic");
        result.RefreshToken.Should().BeNull();
        result.ExpiresAt.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task GetTokenAsync_TrendyolStyleApiKey_ProducesValidBase64()
    {
        // Arrange — Trendyol API keys are alphanumeric with hyphens
        var provider = CreateProvider(
            platformCode: "Trendyol",
            apiKey: "1234567890",
            apiSecret: "abcdef-ghijkl-mnopqr");
        var expectedToken = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("1234567890:abcdef-ghijkl-mnopqr"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be(expectedToken);
        // Verify it is valid Base64
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.AccessToken));
        decoded.Should().Be("1234567890:abcdef-ghijkl-mnopqr");
    }

    [Fact]
    public void IsTokenExpired_AnyToken_AlwaysReturnsFalse()
    {
        // Arrange — ApiKey tokens never expire
        var provider = CreateProvider();
        var pastToken = new AuthToken("tok", null, DateTime.UtcNow.AddDays(-30));
        var futureToken = new AuthToken("tok", null, DateTime.UtcNow.AddYears(100));

        // Act & Assert
        provider.IsTokenExpired(pastToken).Should().BeFalse();
        provider.IsTokenExpired(futureToken).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_AnyInput_ReturnsSameTokenAsGetToken()
    {
        // Arrange — ApiKey auth has no actual refresh concept
        var provider = CreateProvider(apiKey: "key", apiSecret: "secret");

        // Act
        var getResult = await provider.GetTokenAsync();
        var refreshResult = await provider.RefreshTokenAsync("ignored-refresh-token");

        // Assert
        refreshResult.AccessToken.Should().Be(getResult.AccessToken);
        refreshResult.TokenType.Should().Be(getResult.TokenType);
        refreshResult.ExpiresAt.Should().Be(getResult.ExpiresAt);
    }

    [Fact]
    public void PlatformCode_ReturnsCiceksepetiWhenConfigured()
    {
        // Arrange
        var provider = CreateProvider(platformCode: "Ciceksepeti");

        // Act & Assert
        provider.PlatformCode.Should().Be("Ciceksepeti");
    }

    [Fact]
    public async Task GetTokenAsync_DifferentPlatforms_ProduceSameFormatToken()
    {
        // Arrange — same credentials, different platform codes produce same token
        var trendyol = new ApiKeyAuthProvider("Trendyol", "key", "secret");
        var pazarama = new ApiKeyAuthProvider("Pazarama", "key", "secret");

        // Act
        var trendyolToken = await trendyol.GetTokenAsync();
        var pazaramaToken = await pazarama.GetTokenAsync();

        // Assert — platform code does not affect token value
        trendyolToken.AccessToken.Should().Be(pazaramaToken.AccessToken);
    }

    [Fact]
    public async Task GetTokenAsync_EmptyApiKey_ReturnsBase64OfColonAndSecret()
    {
        // Arrange
        var provider = CreateProvider(apiKey: "", apiSecret: "only-secret");
        var expectedToken = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(":only-secret"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be(expectedToken);
    }
}
