using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;

namespace MesTech.Tests.Integration.Auth;

/// <summary>
/// BasicAuthProvider integration tests.
/// Tests Base64 encoding, credential handling, token properties.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Basic")]
public class BasicAuthProviderTests
{
  private const string TestPlatformCode = "Hepsiburada";
  private const string TestUsername = "merchant@example.com";
  private const string TestPassword = "s3cret-p@ss!";

  private BasicAuthProvider CreateProvider(
    string platformCode = TestPlatformCode,
    string username = TestUsername,
    string password = TestPassword)
  {
    return new BasicAuthProvider(platformCode, username, password);
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

    // Decode and verify the Base64 contains username:password
    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be($"{TestUsername}:{TestPassword}");
  }

  [Fact]
  public async Task GetTokenAsync_ReturnsMaxDateTimeExpiry()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert — Basic auth tokens never expire
    token.ExpiresAt.Should().Be(DateTime.MaxValue);
    token.RefreshToken.Should().BeNull();
  }

  [Fact]
  public async Task GetTokenAsync_DifferentCredentials_ProducesDifferentTokens()
  {
    // Arrange
    var provider1 = CreateProvider(username: "user-a", password: "pass-a");
    var provider2 = CreateProvider(username: "user-b", password: "pass-b");

    // Act
    var token1 = await provider1.GetTokenAsync();
    var token2 = await provider2.GetTokenAsync();

    // Assert
    token1.AccessToken.Should().NotBe(token2.AccessToken);
  }

  // ════════════════════════════════════════════════════════════════
  //  2. Auth Failure Scenario — Empty Credentials
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_EmptyUsername_StillProducesBase64Token()
  {
    // BasicAuthProvider encodes whatever it receives — validation is upstream
    var provider = CreateProvider(username: "", password: "pass");
    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be(":pass");
  }

  [Fact]
  public async Task GetTokenAsync_EmptyPassword_StillProducesBase64Token()
  {
    var provider = CreateProvider(username: "user", password: "");
    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be("user:");
  }

  [Fact]
  public async Task GetTokenAsync_SpecialCharacters_EncodesCorrectly()
  {
    // Turkish special chars and symbols
    var provider = CreateProvider(
      username: "kullanici@sirket.com.tr",
      password: "Güçlü$ifre123!");

    var token = await provider.GetTokenAsync();

    var decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(token.AccessToken));
    decoded.Should().Be("kullanici@sirket.com.tr:Güçlü$ifre123!");
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
    var refreshToken = await provider.RefreshTokenAsync("any-refresh-token");

    // Assert — Basic auth refresh just calls GetToken
    refreshToken.AccessToken.Should().Be(getToken.AccessToken);
    refreshToken.TokenType.Should().Be("Basic");
    refreshToken.ExpiresAt.Should().Be(DateTime.MaxValue);
  }

  [Fact]
  public async Task RefreshTokenAsync_IgnoresRefreshTokenParameter()
  {
    // Basic auth doesn't use refresh tokens at all
    var provider = CreateProvider();

    var result1 = await provider.RefreshTokenAsync("token-a");
    var result2 = await provider.RefreshTokenAsync("token-b");

    result1.AccessToken.Should().Be(result2.AccessToken);
  }

  // ════════════════════════════════════════════════════════════════
  //  4. IsTokenExpired — Always False
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void IsTokenExpired_AnyToken_ReturnsFalse()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "anything",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(-100),
      TokenType: "Basic");

    provider.IsTokenExpired(token).Should().BeFalse();
  }

  [Fact]
  public void IsTokenExpired_FutureExpiry_ReturnsFalse()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "anything",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddHours(24),
      TokenType: "Basic");

    provider.IsTokenExpired(token).Should().BeFalse();
  }

  // ════════════════════════════════════════════════════════════════
  //  5. PlatformCode Property
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void PlatformCode_ReturnsConfiguredValue()
  {
    var provider = CreateProvider(platformCode: "CustomPlatform");
    provider.PlatformCode.Should().Be("CustomPlatform");
  }

  [Fact]
  public void PlatformCode_DefaultTestValue()
  {
    var provider = CreateProvider();
    provider.PlatformCode.Should().Be(TestPlatformCode);
  }
}
