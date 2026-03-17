using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Integration.Tests.Auth;

[Trait("Category", "Integration")]
public class Bitrix24AuthProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly Mock<ITokenCacheProvider> _tokenCacheMock;
    private readonly NullLogger<Bitrix24AuthProvider> _logger;

    public Bitrix24AuthProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _tokenCacheMock = new Mock<ITokenCacheProvider>();
        _logger = NullLogger<Bitrix24AuthProvider>.Instance;
    }

    private Bitrix24AuthProvider CreateConfiguredProvider(
        string portal = "mycompany.bitrix24.com",
        string refreshToken = "initial-refresh-token",
        string? customTokenEndpoint = null)
    {
        var httpClient = new HttpClient();
        var provider = new Bitrix24AuthProvider(httpClient, _tokenCacheMock.Object, _logger);
        provider.Configure(
            clientId: "test-client-id",
            clientSecret: "test-client-secret",
            portalDomain: portal,
            refreshToken: refreshToken,
            tokenEndpoint: customTokenEndpoint ?? $"{_wireMock.Url}/oauth/token/");
        return provider;
    }

    private void SetupTokenCache(AuthToken? cached = null)
    {
        _tokenCacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        _tokenCacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetTokenAsync_NoCachedToken_RefreshesAndReturnsNewToken()
    {
        // Arrange
        SetupTokenCache(cached: null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "access_token": "bitrix-access-123",
                        "refresh_token": "bitrix-refresh-456",
                        "expires_in": 1800,
                        "domain": "mycompany.bitrix24.com",
                        "member_id": "abc123"
                    }
                    """));

        var provider = CreateConfiguredProvider();

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be("bitrix-access-123");
        result.RefreshToken.Should().Be("bitrix-refresh-456");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _tokenCacheMock.Verify(
            c => c.SetAsync(
                It.Is<string>(k => k.Contains("bitrix24:")),
                It.IsAny<AuthToken>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ValidTokenInCache_ReturnsCachedTokenWithoutHttpCall()
    {
        // Arrange
        var cachedToken = new AuthToken(
            AccessToken: "cached-bitrix-token",
            RefreshToken: "cached-refresh",
            ExpiresAt: DateTime.UtcNow.AddMinutes(20),
            TokenType: "Bearer");

        SetupTokenCache(cached: cachedToken);

        var provider = CreateConfiguredProvider();

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().Be(cachedToken);
        _wireMock.LogEntries.Should().BeEmpty("cache hit should not trigger HTTP call");
    }

    [Fact]
    public async Task GetTokenAsync_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange — provider without Configure() call
        var httpClient = new HttpClient();
        var provider = new Bitrix24AuthProvider(httpClient, _tokenCacheMock.Object, _logger);

        // Act
        var act = () => provider.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task GetTokenAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        // Arrange
        SetupTokenCache(cached: null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("""{"error":"invalid_token","error_description":"Token is invalid"}"""));

        var provider = CreateConfiguredProvider();

        // Act
        var act = () => provider.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Bitrix24 token request failed*");
    }

    [Fact]
    public async Task RefreshTokenAsync_ExplicitRefreshToken_PostsRefreshGrantAndReturnsToken()
    {
        // Arrange
        SetupTokenCache(cached: null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "access_token": "refreshed-access",
                        "refresh_token": "rotated-refresh-token",
                        "expires_in": 1800
                    }
                    """));

        var provider = CreateConfiguredProvider();

        // Act
        var result = await provider.RefreshTokenAsync("my-current-refresh-token");

        // Assert
        result.AccessToken.Should().Be("refreshed-access");
        result.RefreshToken.Should().Be("rotated-refresh-token");

        // Verify grant_type=refresh_token was sent
        var requestBody = _wireMock.LogEntries.Last().RequestMessage.Body;
        requestBody.Should().Contain("grant_type=refresh_token");
        requestBody.Should().Contain("refresh_token=my-current-refresh-token");
    }

    [Fact]
    public async Task GetTokenAsync_ServerReturnsNewRefreshToken_RotatesInternalRefreshToken()
    {
        // Arrange
        SetupTokenCache(cached: null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "access_token": "new-access",
                        "refresh_token": "new-rotated-refresh",
                        "expires_in": 1800
                    }
                    """));

        var provider = CreateConfiguredProvider(refreshToken: "old-refresh");

        // Act
        var result = await provider.GetTokenAsync();

        // Assert — token rotation: new refresh_token returned in result
        result.RefreshToken.Should().Be("new-rotated-refresh");
    }

    [Fact]
    public async Task ExchangeAuthCodeAsync_ValidCode_ReturnsTokenAndStoresInCache()
    {
        // Arrange
        SetupTokenCache(cached: null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "access_token": "initial-access",
                        "refresh_token": "initial-refresh",
                        "expires_in": 1800
                    }
                    """));

        var provider = CreateConfiguredProvider();

        // Act
        var result = await provider.ExchangeAuthCodeAsync(
            authCode: "auth-code-from-b24",
            redirectUri: "https://myapp.com/callback");

        // Assert
        result.AccessToken.Should().Be("initial-access");

        var requestBody = _wireMock.LogEntries.Last().RequestMessage.Body;
        requestBody.Should().Contain("grant_type=authorization_code");
        requestBody.Should().Contain("code=auth-code-from-b24");
        requestBody.Should().Contain("redirect_uri=");

        _tokenCacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void IsTokenExpired_TokenExpiringWithin5Minutes_ReturnsTrue()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new Bitrix24AuthProvider(httpClient, _tokenCacheMock.Object, _logger);
        var nearExpiry = new AuthToken("tok", null, DateTime.UtcNow.AddMinutes(4));

        // Act & Assert
        provider.IsTokenExpired(nearExpiry).Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_TokenExpiringIn30Minutes_ReturnsFalse()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new Bitrix24AuthProvider(httpClient, _tokenCacheMock.Object, _logger);
        var validToken = new AuthToken("tok", null, DateTime.UtcNow.AddMinutes(30));

        // Act & Assert
        provider.IsTokenExpired(validToken).Should().BeFalse();
    }

    [Fact]
    public void PlatformCode_AlwaysReturnsBitrix24()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new Bitrix24AuthProvider(httpClient, _tokenCacheMock.Object, _logger);

        // Act & Assert
        provider.PlatformCode.Should().Be("Bitrix24");
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bitrix24AuthProvider(null!, _tokenCacheMock.Object, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_NullTokenCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bitrix24AuthProvider(new HttpClient(), null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("tokenCache");
    }

    public void Dispose() => _wireMock.Dispose();
}
