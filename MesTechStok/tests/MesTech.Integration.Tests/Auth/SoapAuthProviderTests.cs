using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Integration.Tests.Auth;

[Trait("Category", "Unit")]
public class SoapAuthProviderTests
{
    private static SoapAuthProvider CreateProvider(
        string platformCode = "N11",
        string appKey = "test-app-key",
        string appSecret = "test-app-secret")
        => new(platformCode, appKey, appSecret, NullLogger<SoapAuthProvider>.Instance);

    [Fact]
    public async Task GetTokenAsync_ValidAppKeyAndSecret_ReturnsConcatenatedToken()
    {
        // Arrange
        var provider = CreateProvider(appKey: "n11-app-key", appSecret: "n11-app-secret");

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("n11-app-key:n11-app-secret");
        result.TokenType.Should().Be("SOAP");
        result.RefreshToken.Should().BeNull();
        result.ExpiresAt.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task GetTokenAsync_WithColonInSecret_PreservesRawConcatenation()
    {
        // Arrange — N11 app secrets may contain special chars
        var provider = CreateProvider(appKey: "key123", appSecret: "sec:ret");

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be("key123:sec:ret");
    }

    [Fact]
    public void IsTokenExpired_AnyToken_AlwaysReturnsFalse()
    {
        // Arrange — SOAP tokens (static appKey:appSecret) never expire
        var provider = CreateProvider();
        var pastToken = new AuthToken("tok", null, DateTime.UtcNow.AddYears(-1));
        var nowToken = new AuthToken("tok", null, DateTime.UtcNow);

        // Act & Assert
        provider.IsTokenExpired(pastToken).Should().BeFalse();
        provider.IsTokenExpired(nowToken).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_AnyInput_ReturnsSameAsGetToken()
    {
        // Arrange
        var provider = CreateProvider(appKey: "key", appSecret: "secret");

        // Act
        var getResult = await provider.GetTokenAsync();
        var refreshResult = await provider.RefreshTokenAsync("irrelevant-refresh");

        // Assert
        refreshResult.AccessToken.Should().Be(getResult.AccessToken);
        refreshResult.TokenType.Should().Be(getResult.TokenType);
    }

    [Fact]
    public void PlatformCode_ReturnsN11WhenConfigured()
    {
        // Arrange
        var provider = CreateProvider(platformCode: "N11");

        // Act & Assert
        provider.PlatformCode.Should().Be("N11");
    }

    [Fact]
    public async Task GetTokenAsync_EmptyCredentials_ReturnsColonToken()
    {
        // Arrange
        var provider = CreateProvider(appKey: "", appSecret: "");

        // Act
        var result = await provider.GetTokenAsync();

        // Assert — format is always appKey:appSecret even when both empty
        result.AccessToken.Should().Be(":");
    }

    [Fact]
    public async Task GetTokenAsync_IsSynchronous_CompletesImmediately()
    {
        // Arrange — SoapAuthProvider does not make HTTP calls
        var provider = CreateProvider();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act & Assert — should complete well within cancellation window
        var result = await provider.GetTokenAsync(cts.Token);
        result.Should().NotBeNull();
    }
}
