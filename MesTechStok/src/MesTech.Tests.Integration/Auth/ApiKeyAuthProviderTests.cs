using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;

namespace MesTech.Tests.Integration.Auth;

/// <summary>
/// ApiKeyAuthProvider integration tests.
/// Tests API key + secret Base64 encoding, token stability, and non-expiring behavior.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ApiKey")]
public class ApiKeyAuthProviderTests
{
  private const string TestPlatformCode = "Trendyol";
  private const string TestApiKey = "placeholder-api-key";
  private const string TestApiSecret = "placeholder-api-secret";

  private ApiKeyAuthProvider CreateProvider(
    string platformCode = TestPlatformCode,
    string apiKey = TestApiKey,
    string apiSecret = TestApiSecret)
  {
    return new ApiKeyAuthProvider(platformCode, apiKey, apiSecret);
  }

  // ════════════════════════════════════════════════════════════════
  //  1. GetTokenAsync — Success
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_ValidCredentials_ReturnsBase64EncodedToken()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.Should().NotBeNull();
    token.AccessToken.Should().NotBeNullOrWhiteSpace();
    token.TokenType.Should().Be("Basic");

    // Decode and verify format is apiKey:apiSecret
    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be($"{TestApiKey}:{TestApiSecret}");
  }

  [Fact]
  public async Task GetTokenAsync_ReturnsNonExpiringToken()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.ExpiresAt.Should().Be(DateTime.MaxValue);
    token.RefreshToken.Should().BeNull();
  }

  [Fact]
  public async Task GetTokenAsync_MultipleCalls_ReturnsSameToken()
  {
    // Arrange — API key tokens are deterministic
    var provider = CreateProvider();

    // Act
    var token1 = await provider.GetTokenAsync();
    var token2 = await provider.GetTokenAsync();

    // Assert
    token1.AccessToken.Should().Be(token2.AccessToken);
  }

  [Fact]
  public async Task GetTokenAsync_DifferentPlatforms_ProduceDifferentTokens()
  {
    // Arrange
    var trendyolProvider = CreateProvider(apiKey: "trendyol-key", apiSecret: "trendyol-secret");
    var ciceksepetiProvider = CreateProvider(
      platformCode: "Ciceksepeti",
      apiKey: "cicek-key",
      apiSecret: "cicek-secret");

    // Act
    var token1 = await trendyolProvider.GetTokenAsync();
    var token2 = await ciceksepetiProvider.GetTokenAsync();

    // Assert
    token1.AccessToken.Should().NotBe(token2.AccessToken);
  }

  // ════════════════════════════════════════════════════════════════
  //  2. Auth Failure Scenario — Empty/Invalid Keys
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_EmptyApiKey_StillEncodesBase64()
  {
    // ApiKeyAuthProvider encodes whatever it receives — validation is upstream
    var provider = CreateProvider(apiKey: "", apiSecret: "secret");
    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be(":secret");
  }

  [Fact]
  public async Task GetTokenAsync_EmptyApiSecret_StillEncodesBase64()
  {
    var provider = CreateProvider(apiKey: "key", apiSecret: "");
    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be("key:");
  }

  [Fact]
  public async Task GetTokenAsync_SpecialCharactersInKey_EncodesCorrectly()
  {
    var provider = CreateProvider(
      apiKey: "key/with+special=chars",
      apiSecret: "secret&more=special!");

    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be("key/with+special=chars:secret&more=special!");
  }

  // ════════════════════════════════════════════════════════════════
  //  3. RefreshTokenAsync — Delegates to GetTokenAsync
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task RefreshTokenAsync_ReturnsSameTokenAsGetToken()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var getToken = await provider.GetTokenAsync();
    var refreshToken = await provider.RefreshTokenAsync("any-refresh-value");

    // Assert
    refreshToken.AccessToken.Should().Be(getToken.AccessToken);
    refreshToken.TokenType.Should().Be("Basic");
    refreshToken.ExpiresAt.Should().Be(DateTime.MaxValue);
  }

  [Fact]
  public async Task RefreshTokenAsync_IgnoresRefreshTokenParameter()
  {
    var provider = CreateProvider();

    var result1 = await provider.RefreshTokenAsync("token-a");
    var result2 = await provider.RefreshTokenAsync("token-b");
    var result3 = await provider.RefreshTokenAsync("");

    result1.AccessToken.Should().Be(result2.AccessToken);
    result2.AccessToken.Should().Be(result3.AccessToken);
  }

  // ════════════════════════════════════════════════════════════════
  //  4. IsTokenExpired — Always False
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void IsTokenExpired_AnyToken_ReturnsFalse()
  {
    var provider = CreateProvider();

    // Even a token with a past expiry should return false for API key auth
    var pastToken = new AuthToken(
      AccessToken: "anything",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddDays(-30),
      TokenType: "Basic");

    provider.IsTokenExpired(pastToken).Should().BeFalse();
  }

  [Fact]
  public void IsTokenExpired_FutureToken_ReturnsFalse()
  {
    var provider = CreateProvider();
    var futureToken = new AuthToken(
      AccessToken: "anything",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddYears(10),
      TokenType: "Basic");

    provider.IsTokenExpired(futureToken).Should().BeFalse();
  }

  // ════════════════════════════════════════════════════════════════
  //  5. PlatformCode Property
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void PlatformCode_ReturnsConfiguredValue()
  {
    var provider = CreateProvider(platformCode: "Pazarama");
    provider.PlatformCode.Should().Be("Pazarama");
  }

  [Theory]
  [InlineData("Trendyol")]
  [InlineData("Ciceksepeti")]
  [InlineData("Ozon")]
  [InlineData("Pazarama")]
  [InlineData("PttAVM")]
  public void PlatformCode_SupportsAllExpectedPlatforms(string platform)
  {
    var provider = CreateProvider(platformCode: platform);
    provider.PlatformCode.Should().Be(platform);
  }
}
