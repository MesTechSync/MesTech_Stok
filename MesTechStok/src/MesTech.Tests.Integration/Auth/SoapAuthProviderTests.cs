using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Integration.Auth;

/// <summary>
/// SoapAuthProvider integration tests.
/// Tests SOAP-specific token generation for N11 platform.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "SOAP")]
public class SoapAuthProviderTests
{
  private const string TestPlatformCode = "N11";
  private const string TestAppKey = "n11-app-key-abc";
  private const string TestAppSecret = "n11-app-secret-xyz";

  private SoapAuthProvider CreateProvider(
    string platformCode = TestPlatformCode,
    string appKey = TestAppKey,
    string appSecret = TestAppSecret)
  {
    var logger = NullLogger<SoapAuthProvider>.Instance;
    return new SoapAuthProvider(platformCode, appKey, appSecret, logger);
  }

  // ════════════════════════════════════════════════════════════════
  //  1. GetTokenAsync — Success
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_ValidCredentials_ReturnsAppKeyColonSecret()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.Should().NotBeNull();
    token.AccessToken.Should().Be($"{TestAppKey}:{TestAppSecret}");
    token.TokenType.Should().Be("SOAP");
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
  public async Task GetTokenAsync_MultipleCalls_ReturnsDeterministicResult()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var token1 = await provider.GetTokenAsync();
    var token2 = await provider.GetTokenAsync();

    // Assert
    token1.AccessToken.Should().Be(token2.AccessToken);
  }

  [Fact]
  public async Task GetTokenAsync_TokenContainsBothAppKeyAndSecret()
  {
    // Arrange
    var provider = CreateProvider(appKey: "MY-KEY", appSecret: "MY-SECRET");

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.AccessToken.Should().Contain("MY-KEY");
    token.AccessToken.Should().Contain("MY-SECRET");
    token.AccessToken.Should().Contain(":");
  }

  // ════════════════════════════════════════════════════════════════
  //  2. Auth Failure Scenario — Empty Credentials
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_EmptyAppKey_StillFormatsToken()
  {
    // SoapAuthProvider concatenates credentials — validation is upstream
    var provider = CreateProvider(appKey: "", appSecret: "secret");
    var token = await provider.GetTokenAsync();

    token.AccessToken.Should().Be(":secret");
  }

  [Fact]
  public async Task GetTokenAsync_EmptyAppSecret_StillFormatsToken()
  {
    var provider = CreateProvider(appKey: "key", appSecret: "");
    var token = await provider.GetTokenAsync();

    token.AccessToken.Should().Be("key:");
  }

  [Fact]
  public async Task GetTokenAsync_BothEmpty_ReturnsColonOnly()
  {
    var provider = CreateProvider(appKey: "", appSecret: "");
    var token = await provider.GetTokenAsync();

    token.AccessToken.Should().Be(":");
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

    // Assert
    refreshToken.AccessToken.Should().Be(getToken.AccessToken);
    refreshToken.TokenType.Should().Be("SOAP");
    refreshToken.ExpiresAt.Should().Be(DateTime.MaxValue);
  }

  [Fact]
  public async Task RefreshTokenAsync_IgnoresRefreshTokenValue()
  {
    var provider = CreateProvider();

    var result1 = await provider.RefreshTokenAsync("token-x");
    var result2 = await provider.RefreshTokenAsync("token-y");

    result1.AccessToken.Should().Be(result2.AccessToken);
  }

  // ════════════════════════════════════════════════════════════════
  //  4. IsTokenExpired — Always False
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void IsTokenExpired_AnyToken_ReturnsFalse()
  {
    var provider = CreateProvider();

    var expiredToken = new AuthToken(
      AccessToken: "key:secret",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddDays(-365),
      TokenType: "SOAP");

    provider.IsTokenExpired(expiredToken).Should().BeFalse();
  }

  [Fact]
  public void IsTokenExpired_FutureToken_ReturnsFalse()
  {
    var provider = CreateProvider();
    var futureToken = new AuthToken(
      AccessToken: "key:secret",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddYears(10),
      TokenType: "SOAP");

    provider.IsTokenExpired(futureToken).Should().BeFalse();
  }

  // ════════════════════════════════════════════════════════════════
  //  5. PlatformCode Property
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void PlatformCode_ReturnsConfiguredValue()
  {
    var provider = CreateProvider(platformCode: "N11");
    provider.PlatformCode.Should().Be("N11");
  }

  [Fact]
  public void PlatformCode_CustomPlatform()
  {
    var provider = CreateProvider(platformCode: "CustomSOAP");
    provider.PlatformCode.Should().Be("CustomSOAP");
  }
}
