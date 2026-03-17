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
public class OAuth2AuthProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly Mock<ITokenCacheProvider> _tokenCacheMock;
    private readonly NullLogger<OAuth2AuthProvider> _logger;

    public OAuth2AuthProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _tokenCacheMock = new Mock<ITokenCacheProvider>();
        _logger = NullLogger<OAuth2AuthProvider>.Instance;
    }

    private OAuth2AuthProvider CreateProvider(string tokenPath = "/oauth/token", string? scope = null)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        return new OAuth2AuthProvider(
            platformCode: "TestPlatform",
            httpClient: httpClient,
            tokenCache: _tokenCacheMock.Object,
            clientId: "test-client-id",
            clientSecret: "test-client-secret",
            tokenEndpoint: $"{_wireMock.Url}{tokenPath}",
            scope: scope,
            logger: _logger);
    }

    [Fact]
    public async Task GetTokenAsync_NoCachedToken_RequestsAndReturnsNewToken()
    {
        // Arrange
        _tokenCacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthToken?)null);
        _tokenCacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"access_token":"token-abc","expires_in":3600,"token_type":"Bearer"}"""));

        var provider = CreateProvider();

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("token-abc");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _tokenCacheMock.Verify(
            c => c.SetAsync("oauth2:TestPlatform", It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ValidTokenInCache_ReturnsCachedTokenWithoutHttpCall()
    {
        // Arrange
        var cachedToken = new AuthToken(
            AccessToken: "cached-token",
            RefreshToken: null,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            TokenType: "Bearer");

        _tokenCacheMock
            .Setup(c => c.GetAsync("oauth2:TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedToken);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().Be(cachedToken);
        _wireMock.LogEntries.Should().BeEmpty("no HTTP request should be made when cache is valid");
    }

    [Fact]
    public async Task GetTokenAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        // Arrange
        _tokenCacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthToken?)null);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("invalid_client"));

        var provider = CreateProvider();

        // Act
        var act = () => provider.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*OAuth2 token request failed*");
    }

    [Fact]
    public async Task GetTokenAsync_WithScope_IncludesScopeInRequest()
    {
        // Arrange
        _tokenCacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthToken?)null);
        _tokenCacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"access_token":"scoped-token","expires_in":3600,"token_type":"Bearer"}"""));

        var provider = CreateProvider(scope: "read:products write:orders");

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be("scoped-token");
        var requestBody = _wireMock.LogEntries.Last().RequestMessage.Body;
        requestBody.Should().Contain("scope=read");
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewToken()
    {
        // Arrange
        _tokenCacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AuthToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _wireMock
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"access_token":"refreshed-token","refresh_token":"new-refresh","expires_in":3600,"token_type":"Bearer"}"""));

        var provider = CreateProvider();

        // Act
        var result = await provider.RefreshTokenAsync("old-refresh-token");

        // Assert
        result.AccessToken.Should().Be("refreshed-token");
        result.RefreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public void IsTokenExpired_TokenExpiringInMoreThan5Minutes_ReturnsFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var token = new AuthToken("tok", null, DateTime.UtcNow.AddHours(1));

        // Act & Assert
        provider.IsTokenExpired(token).Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_TokenExpiringInLessThan5Minutes_ReturnsTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var token = new AuthToken("tok", null, DateTime.UtcNow.AddMinutes(3));

        // Act & Assert
        provider.IsTokenExpired(token).Should().BeTrue();
    }

    [Fact]
    public void PlatformCode_ReturnsConfiguredValue()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        provider.PlatformCode.Should().Be("TestPlatform");
    }

    public void Dispose() => _wireMock.Dispose();
}
