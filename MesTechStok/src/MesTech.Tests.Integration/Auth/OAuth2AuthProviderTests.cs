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
/// OAuth2AuthProvider integration tests — WireMock based.
/// Tests client_credentials grant, token caching, refresh flow, and error handling.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "OAuth2")]
public class OAuth2AuthProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
  private readonly WireMockFixture _fixture;
  private readonly WireMockServer _mockServer;
  private readonly InMemoryTokenCacheProvider _tokenCache;
  private readonly ILogger<OAuth2AuthProvider> _logger;

  private const string TestClientId = "test-client-id";
  private const string TestClientSecret = "test-client-secret";
  private const string TestPlatformCode = "Amazon";

  public OAuth2AuthProviderTests(WireMockFixture fixture)
  {
    _fixture = fixture;
    _fixture.Reset();
    _mockServer = fixture.Server;
    _tokenCache = new InMemoryTokenCacheProvider();
    _logger = NullLogger<OAuth2AuthProvider>.Instance;
  }

  public void Dispose()
  {
    _fixture.Reset();
  }

  private OAuth2AuthProvider CreateProvider(string? scope = null)
  {
    var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
    var tokenEndpoint = _fixture.BaseUrl + "/oauth/token";
    return new OAuth2AuthProvider(
      TestPlatformCode,
      httpClient,
      _tokenCache,
      TestClientId,
      TestClientSecret,
      tokenEndpoint,
      scope,
      _logger);
  }

  private void StubTokenEndpoint(
    string accessToken = "test-access-token-abc123",
    int expiresIn = 3600,
    string? refreshToken = null,
    string tokenType = "Bearer")
  {
    var refreshPart = refreshToken is not null
      ? $@",""refresh_token"":""{refreshToken}"""
      : "";

    _mockServer
      .Given(Request.Create()
        .WithPath("/oauth/token")
        .UsingPost())
      .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody($@"{{
          ""access_token"":""{accessToken}"",
          ""token_type"":""{tokenType}"",
          ""expires_in"":{expiresIn}{refreshPart}
        }}"));
  }

  private void StubTokenEndpointError(int statusCode, string errorBody)
  {
    _mockServer
      .Given(Request.Create()
        .WithPath("/oauth/token")
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
  public async Task GetTokenAsync_ValidCredentials_ReturnsAccessToken()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "fresh-token-xyz", expiresIn: 7200);
    var provider = CreateProvider(scope: "api:read");

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.Should().NotBeNull();
    token.AccessToken.Should().Be("fresh-token-xyz");
    token.TokenType.Should().Be("Bearer");
    token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public async Task GetTokenAsync_WithScope_IncludesScopeInRequest()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "scoped-token");
    var provider = CreateProvider(scope: "catalog:read orders:write");

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.AccessToken.Should().Be("scoped-token");
    // Verify at least one request was made to the token endpoint
    _mockServer.LogEntries.Should().Contain(e =>
      e.RequestMessage.Path!.Contains("/oauth/token"));
  }

  [Fact]
  public async Task GetTokenAsync_CachesToken_SecondCallDoesNotHitEndpoint()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "cached-token", expiresIn: 7200);
    var provider = CreateProvider();

    // Act — first call: fetches from server
    var token1 = await provider.GetTokenAsync();
    var requestCountAfterFirst = _mockServer.LogEntries.Count();

    // Act — second call: should come from cache
    var token2 = await provider.GetTokenAsync();
    var requestCountAfterSecond = _mockServer.LogEntries.Count();

    // Assert
    token1.AccessToken.Should().Be("cached-token");
    token2.AccessToken.Should().Be("cached-token");
    requestCountAfterSecond.Should().Be(requestCountAfterFirst,
      "second call should use cached token, not make another HTTP request");
  }

  // ════════════════════════════════════════════════════════════════
  //  2. GetTokenAsync — Auth Failure
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_Unauthorized401_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(401, @"{""error"":""invalid_client"",""error_description"":""Bad credentials""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*Unauthorized*");
  }

  [Fact]
  public async Task GetTokenAsync_Forbidden403_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(403, @"{""error"":""insufficient_scope""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*Forbidden*");
  }

  [Fact]
  public async Task GetTokenAsync_ServerError500_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(500, @"{""error"":""internal_server_error""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.GetTokenAsync();

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*InternalServerError*");
  }

  // ════════════════════════════════════════════════════════════════
  //  3. RefreshTokenAsync — Token Refresh Flow
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewAccessToken()
  {
    // Arrange
    StubTokenEndpoint(
      accessToken: "refreshed-token-new",
      expiresIn: 3600,
      refreshToken: "new-refresh-token");
    var provider = CreateProvider();

    // Act
    var token = await provider.RefreshTokenAsync("old-refresh-token-abc");

    // Assert
    token.AccessToken.Should().Be("refreshed-token-new");
    token.RefreshToken.Should().Be("new-refresh-token");
    token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public async Task RefreshTokenAsync_InvalidRefreshToken_ThrowsHttpRequestException()
  {
    // Arrange
    StubTokenEndpointError(400, @"{""error"":""invalid_grant"",""error_description"":""Refresh token expired""}");
    var provider = CreateProvider();

    // Act
    Func<Task> act = async () => await provider.RefreshTokenAsync("expired-refresh-token");

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*BadRequest*");
  }

  [Fact]
  public async Task RefreshTokenAsync_CachesNewToken()
  {
    // Arrange
    StubTokenEndpoint(accessToken: "refreshed-cached", expiresIn: 7200);
    var provider = CreateProvider();

    // Act
    await provider.RefreshTokenAsync("some-refresh-token");

    // Assert — verify cached via GetTokenAsync (should not make another HTTP call)
    var requestCountBeforeGet = _mockServer.LogEntries.Count();
    var cachedToken = await provider.GetTokenAsync();
    var requestCountAfterGet = _mockServer.LogEntries.Count();

    cachedToken.AccessToken.Should().Be("refreshed-cached");
    requestCountAfterGet.Should().Be(requestCountBeforeGet,
      "GetTokenAsync should return the cached refreshed token");
  }

  // ════════════════════════════════════════════════════════════════
  //  4. IsTokenExpired — Expiry Logic
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void IsTokenExpired_TokenExpiresInFuture_ReturnsFalse()
  {
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "valid",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddHours(1),
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
  public void IsTokenExpired_TokenWithinRefreshBuffer_ReturnsTrue()
  {
    // RefreshBuffer is 5 minutes — a token expiring in 4 minutes should be "expired"
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "almost-expired",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(4),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeTrue();
  }

  [Fact]
  public void IsTokenExpired_TokenExpiresAfterBuffer_ReturnsFalse()
  {
    // RefreshBuffer is 5 minutes — a token expiring in 10 minutes should NOT be expired
    var provider = CreateProvider();
    var token = new AuthToken(
      AccessToken: "still-valid",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(10),
      TokenType: "Bearer");

    provider.IsTokenExpired(token).Should().BeFalse();
  }

  // ════════════════════════════════════════════════════════════════
  //  5. PlatformCode Property
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public void PlatformCode_ReturnsConfiguredValue()
  {
    var provider = CreateProvider();
    provider.PlatformCode.Should().Be(TestPlatformCode);
  }

  // ════════════════════════════════════════════════════════════════
  //  6. GetTokenAsync — Expired Cache Forces Refresh
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetTokenAsync_CachedTokenExpired_FetchesNewToken()
  {
    // Arrange — seed cache with an expired token
    var expiredToken = new AuthToken(
      AccessToken: "old-expired-token",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddMinutes(-10),
      TokenType: "Bearer");
    await _tokenCache.SetAsync($"oauth2:{TestPlatformCode}", expiredToken);

    StubTokenEndpoint(accessToken: "brand-new-token", expiresIn: 3600);
    var provider = CreateProvider();

    // Act
    var token = await provider.GetTokenAsync();

    // Assert
    token.AccessToken.Should().Be("brand-new-token");
    _mockServer.LogEntries.Should().HaveCountGreaterOrEqualTo(1,
      "should have made a request to refresh the expired token");
  }
}
