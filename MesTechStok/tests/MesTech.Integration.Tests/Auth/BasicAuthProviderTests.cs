using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Xunit;

namespace MesTech.Integration.Tests.Auth;

[Trait("Category", "Unit")]
public class BasicAuthProviderTests
{
    private static BasicAuthProvider CreateProvider(
        string platformCode = "Hepsiburada",
        string username = "test-user",
        string password = "test-pass")
        => new(platformCode, username, password);

    [Fact]
    public async Task GetTokenAsync_ValidCredentials_ReturnsBase64EncodedToken()
    {
        // Arrange
        var provider = CreateProvider(username: "myuser", password: "mypass");
        var expectedCredentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("myuser:mypass"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedCredentials);
        result.TokenType.Should().Be("Basic");
        result.RefreshToken.Should().BeNull();
        result.ExpiresAt.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task GetTokenAsync_EmptyCredentials_ReturnsBase64OfEmptyColon()
    {
        // Arrange
        var provider = CreateProvider(username: "", password: "");
        var expectedCredentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(":"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be(expectedCredentials);
    }

    [Fact]
    public async Task GetTokenAsync_SpecialCharactersInCredentials_EncodesCorrectly()
    {
        // Arrange — credentials with special characters that are common in API keys
        var provider = CreateProvider(username: "user@domain.com", password: "p@ssw0rd!#%");
        var expectedCredentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("user@domain.com:p@ssw0rd!#%"));

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        result.AccessToken.Should().Be(expectedCredentials);
    }

    [Fact]
    public void IsTokenExpired_AnyToken_AlwaysReturnsFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var expiredToken = new AuthToken("tok", null, DateTime.UtcNow.AddDays(-1));
        var activeToken = new AuthToken("tok", null, DateTime.UtcNow.AddHours(1));
        var maxToken = new AuthToken("tok", null, DateTime.MaxValue);

        // Act & Assert — BasicAuth tokens never expire
        provider.IsTokenExpired(expiredToken).Should().BeFalse();
        provider.IsTokenExpired(activeToken).Should().BeFalse();
        provider.IsTokenExpired(maxToken).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_AnyRefreshToken_ReturnsSameAsGetToken()
    {
        // Arrange
        var provider = CreateProvider(username: "user", password: "pass");

        // Act
        var getResult = await provider.GetTokenAsync();
        var refreshResult = await provider.RefreshTokenAsync("any-refresh-token");

        // Assert
        refreshResult.AccessToken.Should().Be(getResult.AccessToken);
        refreshResult.TokenType.Should().Be(getResult.TokenType);
    }

    [Fact]
    public void PlatformCode_ReturnsConfiguredValue()
    {
        // Arrange
        var provider = CreateProvider(platformCode: "Hepsiburada");

        // Act & Assert
        provider.PlatformCode.Should().Be("Hepsiburada");
    }

    [Fact]
    public async Task GetTokenAsync_MultipleCalls_ReturnsDeterministicToken()
    {
        // Arrange — BasicAuth should always return same token for same credentials
        var provider = CreateProvider(username: "user", password: "pass");

        // Act
        var result1 = await provider.GetTokenAsync();
        var result2 = await provider.GetTokenAsync();

        // Assert
        result1.AccessToken.Should().Be(result2.AccessToken);
    }
}
