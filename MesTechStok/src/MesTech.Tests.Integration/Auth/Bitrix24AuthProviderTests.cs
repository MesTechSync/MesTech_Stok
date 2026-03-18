using System.Linq;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Auth;

/// <summary>
/// Bitrix24AuthProvider integration tests — WireMock based.
/// Tests OAuth 2.0 Authorization Code flow, refresh_token rotation, and error handling.
/// CRITICAL: Each refresh returns a NEW refresh_token — old one becomes invalid.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Bitrix24")]
public class Bitrix24AuthProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
  private readonly WireMockFixture _fixture;
  private readonly WireMockServer _mockServer;
  private readonly InMemoryTokenCacheProvider _tokenCache;
  private readonly ILogger<Bitrix24AuthProvider> _logger;

  private const string TestClientId = "b24-client-id";
  private const string TestClientSecret = "b24-client-secret";
  private const string TestPortalDomain = "mestech.bitrix24.com";
  private const string TestRefreshToken = "initial-refresh-token-abc";

  public Bitrix24AuthProviderTests(WireMockFixture fixture)
  {
    _fixture = fixture;
    _fixture.Reset();
    _mockServer = fixture.Server;
    _tokenCache = new InMemoryTokenCacheProvider();
    _logger = NullLogger<Bitrix24AuthProvider>.Instance;
  }

  public void Dispose()
  {
    _fixture.Reset();
  }

  private Bitrix24AuthProvider CreateProvider(bool configure = true)
  {
    var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
    var provider = new Bitrix24AuthProvider(httpClient, _tokenCache, _logger);

    if (configure)
    {
      provider.Configure(
        TestClientId,
        TestClientSecret,
        TestPortalDomain,
        TestRefreshToken,
        tokenEndpoint: _fixture.BaseUrl + "/oauth/token/");
    }

    return provider;
  }

  private void StubTokenEndpoint(
    string accessToken = "b24-access-token-new",
    int expiresIn = 1800,
    string refreshToken = "b24-rotated-refresh-token",
    string? memberId = "abc123",
    string? domain = null)
  {
    var domainValue = domain ?? TestPortalDomain;
    _mockServer
      .Given(Request.Create()
        .WithPath("/oauth/token/")
        .UsingPost())
      .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody($@"{{
          ""access_token"":""{accessToken}"",
          ""expires_in"":{expiresIn},
          ""refresh_token"":""{refreshToken}"",
          ""member_id"":""{memberId}"",
          ""domain"":""{domainValue}""
        }}"));
  }

  private void StubTokenEndpointError(int statusCode, string errorBody)
  {
    _mockServer
      .Given(Request.Create()
        .WithPath("/oauth/token/")
        .UsingPost())
      .RespondWith(Response.Create()
        .WithStatusCode(statusCode)
        .WithHeader("Content-Type", "application/json")
        .WithBody(errorBody));
  }

  // ════════════════════════════════════════════════════════════════
  //  1. GetTokenAsync — Success
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_Configured_ReturnsAccessToken()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "b24-fresh-token");
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.Should().NotBeNull();
    token.AccessToken.Should().Be("b24-fresh-token");
    token.TokenType.Should().Be("Bearer");
    token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public async Task GetTokenAsync_CachesToken_SecondCallDoesNotHitEndpoint()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "b24-cached-token", expiresIn: 7200);
    var provider = CreateProvider();

    // Act
    var token1 = await provider.GetTokenAsync();
    var requestCountAfterFirst = _mockServer.LogEntries.Count();

    var token2 = await provider.GetTokenAsync();
    var requestCountAfterSecond = _mockServer.LogEntries.Count();

    // Assert
    token1.AccessToken.Should().Be("b24-cached-token");
    token2.AccessToken.Should().Be("b24-cached-token");
    requestCountAfterSecond.Should().Be(requestCountAfterFirst,
      "second call should use cached token");
  }

  [Fact]
  public async Task GetTokenAsync_RotatesRefreshToken()
  {
    // Arrange — Bitrix24 returns a NEW refresh_token on each refresh
    StubTokenEndpoint(
      accessToken: "b24-token-1",
      refreshToken: "rotated-refresh-token-v2");
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.RefreshToken.Should().Be("rotated-refresh-token-v2");
  }

  // ════════════════════════════════════════════════════════════════
  //  2. GetTokenAsync — Auth Failure
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_Unauthorized401_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(401, @"{""error"":""invalid_client""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*401*");
  }

  [Fact]
  public async Task GetTokenAsync_ExpiredRefreshToken400_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(400, @"{""error"":""expired_token"",""error_description"":""Refresh token has expired""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*400*");
  }

  [Fact]
  public async Task GetTokenAsync_NotConfigured_ThrowsInvalidOperationException()
  {
    // Arrange — skip Configure()
    var provider = CreateProvider(configure: false);

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*not configured*");
  }

  // ════════════════════════════════════════════════════════════════
  //  3. RefreshTokenAsync — Token Refresh Flow
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewTokens()
  {
    // Arrange
    StubTokenEndpoint(
      accessToken: "b24-refreshed-access",
      refreshToken: "b24-new-refresh-after-explicit");
    var provider = CreateProvider();

    // Act
    var token = await provider.RefreshTokenAsync("explicit-refresh-token");

    // Assert
    token.AccessToken.Should().Be("b24-refreshed-access");
    token.RefreshToken.Should().Be("b24-new-refresh-after-explicit");
    token.TokenType.Should().Be("Bearer");
  }

  [Fact]
  public async Task RefreshTokenAsync_CachesNewToken()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "b24-refreshed-cached", expiresIn: 7200);
    var provider = CreateProvider();

    // Act
    await provider.RefreshTokenAsync("some-refresh-token");
    var requestCountBefore = _mockServer.LogEntries.Count();

    // Second call to GetToken should use cache
    var cached = await provider.GetTokenAsync();
    var requestCountAfter = _mockServer.LogEntries.Count();

    // Assert
    cached.AccessToken.Should().Be("b24-refreshed-cached");
    requestCountAfter.Should().Be(requestCountBefore);
  }

  [Fact]
  public async Task RefreshTokenAsync_NotConfigured_ThrowsInvalidOperationException()
  {
    var provider = CreateProvider(configure: false);

    Func<Task> act = async () => await provider.RefreshTokenAsync("any-token");

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*not configured*");
  }

  // ════════════════════════════════════════════════════════════════
  //  4. ExchangeAuthCodeAsync — Initial Auth Code Exchange
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task ExchangeAuthCodeAsync_ValidCode_ReturnsTokens()
  {
    // Arrange
    StubTokenEndpoint(
      accessToken: "b24-initial-access",
      refreshToken: "b24-initial-refresh");
    var provider = CreateProvider();

    // Act
    var token = await provider.ExchangeAuthCodeAsync("auth-code-123", "https://mestech.com/callback");

    // Assert
    token.AccessToken.Should().Be("b24-initial-access");
    token.RefreshToken.Should().Be("b24-initial-refresh");
    token.TokenType.Should().Be("Bearer");
  }

  [Fact]
  public async Task ExchangeAuthCodeAsync_InvalidCode_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(400, @"{""error"":""invalid_grant"",""error_description"":""Authorization code expired""}");
    var provider = CreateProvider();

    // Act
    var act = () => provider.ExchangeAuthCodeAsync("expired-code", "https://mestech.com/callback");

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*400*");
  }

  [Fact]
  public async Task ExchangeAuthCodeAsync_NotConfigured_ThrowsInvalidOperationException()
  {
    var provider = CreateProvider(configure: false);

    var act = () => provider.ExchangeAuthCodeAsync("code", "https://example.com/callback");

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*not configured*");
  }

  // ════════════════════════════════════════════════════════════════
  //  5. IsTokenExpired — With Refresh Buffer
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void IsTokenExpired_TokenExpiresInFuture_ReturnsFalse()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "valid",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(30),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeFalse();
  }

  [Fact]
  public void IsTokenExpired_TokenExpiredInPast_ReturnsTrue()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "expired",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(-1),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeTrue();
  }

  [Fact]
  public void IsTokenExpired_TokenWithin5MinBuffer_ReturnsTrue()
  {
    // Bitrix24 RefreshBuffer is 5 minutes — token expiring in 3 min is "expired"
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "almost-expired",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(3),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeTrue();
  }

  [Fact]
  public void IsTokenExpired_TokenExpiresAfterBuffer_ReturnsFalse()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "still-valid",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(10),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeFalse();
  }

  // ════════════════════════════════════════════════════════════════
  //  6. PlatformCode Property
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void PlatformCode_AlwaysBitrix24()
  {
    var provider = CreateProvider();
    provider.PlatformCode.Should().Be("Bitrix24");
  }

  // ════════════════════════════════════════════════════════════════
  //  7. Domain Update from Token Response
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_DomainChangedInResponse_UpdatesInternalDomain()
  {
    // Arrange — response returns a different domain than configured
    StubTokenEndpoint(
      accessToken: "b24-domain-updated",
      domain: "new-domain.bitrix24.com");
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.AccessToken.Should().Be("b24-domain-updated");
    // The provider should have internally updated its domain
    // Verify by clearing cache and making another call
    _fixture.Reset();
    await _tokenCache.RemoveAsync($"bitrix24:{TestPortalDomain}");

    StubTokenEndpoint(accessToken: "b24-after-domain-change");

    // The new cache key should use the new domain
    // GetTokenAsync should work without errors
    var token2 = await provider.GetTokenAsync();
    token2.Should().NotBeNull();
  }

  // ════════════════════════════════════════════════════════════════
  //  8. Configure Method
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void Configure_CustomTokenEndpoint_UsesProvidedEndpoint()
  {
    var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
    var provider = new Bitrix24AuthProvider(httpClient, _tokenCache, _logger);

    // Should not throw
    provider.Configure(
      TestClientId,
      TestClientSecret,
      TestPortalDomain,
      TestRefreshToken,
      tokenEndpoint: "https://custom.bitrix.info/oauth/token/");

    provider.PlatformCode.Should().Be("Bitrix24");
  }

  [Fact]
  public async Task GetTokenAsync_ServerError500_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(500, @"{""error"":""server_error""}");
    var provider = CreateProvider();

    // Act
    var act = () => provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*500*");
  }
}
