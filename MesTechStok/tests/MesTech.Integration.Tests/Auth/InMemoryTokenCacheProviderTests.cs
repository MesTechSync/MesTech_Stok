using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;
using Xunit;

namespace MesTech.Integration.Tests.Auth;

[Trait("Category", "Unit")]
public class InMemoryTokenCacheProviderTests
{
    private static InMemoryTokenCacheProvider CreateCache() => new();

    private static AuthToken MakeToken(string accessToken = "test-token", int expiresInSeconds = 3600)
        => new AuthToken(
            AccessToken: accessToken,
            RefreshToken: null,
            ExpiresAt: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            TokenType: "Bearer");

    [Fact]
    public async Task GetAsync_KeyNotInCache_ReturnsNull()
    {
        // Arrange
        var cache = CreateCache();

        // Act
        var result = await cache.GetAsync("nonexistent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSameToken()
    {
        // Arrange
        var cache = CreateCache();
        var token = MakeToken("my-access-token");

        // Act
        await cache.SetAsync("oauth2:platform1", token);
        var result = await cache.GetAsync("oauth2:platform1");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("my-access-token");
        result.ExpiresAt.Should().BeCloseTo(token.ExpiresAt, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetAsync_ExistingKey_OverwritesPreviousToken()
    {
        // Arrange
        var cache = CreateCache();
        var oldToken = MakeToken("old-token");
        var newToken = MakeToken("new-token");

        // Act
        await cache.SetAsync("oauth2:platform1", oldToken);
        await cache.SetAsync("oauth2:platform1", newToken);
        var result = await cache.GetAsync("oauth2:platform1");

        // Assert
        result!.AccessToken.Should().Be("new-token");
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_SubsequentGetReturnsNull()
    {
        // Arrange
        var cache = CreateCache();
        var token = MakeToken();

        await cache.SetAsync("oauth2:platform1", token);

        // Act
        await cache.RemoveAsync("oauth2:platform1");
        var result = await cache.GetAsync("oauth2:platform1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var cache = CreateCache();

        // Act
        var act = () => cache.RemoveAsync("nonexistent-key");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_MultipleKeys_StoredIndependently()
    {
        // Arrange
        var cache = CreateCache();
        var token1 = MakeToken("token-platform1");
        var token2 = MakeToken("token-platform2");
        var token3 = MakeToken("token-bitrix24");

        // Act
        await cache.SetAsync("oauth2:platform1", token1);
        await cache.SetAsync("oauth2:platform2", token2);
        await cache.SetAsync("bitrix24:myportal", token3);

        var result1 = await cache.GetAsync("oauth2:platform1");
        var result2 = await cache.GetAsync("oauth2:platform2");
        var result3 = await cache.GetAsync("bitrix24:myportal");

        // Assert
        result1!.AccessToken.Should().Be("token-platform1");
        result2!.AccessToken.Should().Be("token-platform2");
        result3!.AccessToken.Should().Be("token-bitrix24");
    }

    [Fact]
    public async Task SetAsync_IsThreadSafe_ConcurrentWritesDoNotCorrupt()
    {
        // Arrange
        var cache = CreateCache();
        const int concurrency = 50;

        // Act — concurrent writes to the same key
        var tasks = Enumerable.Range(0, concurrency)
            .Select(i => cache.SetAsync("shared-key", MakeToken($"token-{i}")))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert — cache must be in a valid state (not null, not thrown)
        var result = await cache.GetAsync("shared-key");
        result.Should().NotBeNull();
        result!.AccessToken.Should().StartWith("token-");
    }

    [Fact]
    public async Task GetAsync_AfterRemoveAllKeys_ReturnsNullForAll()
    {
        // Arrange
        var cache = CreateCache();
        await cache.SetAsync("key1", MakeToken("t1"));
        await cache.SetAsync("key2", MakeToken("t2"));

        // Act
        await cache.RemoveAsync("key1");
        await cache.RemoveAsync("key2");

        // Assert
        (await cache.GetAsync("key1")).Should().BeNull();
        (await cache.GetAsync("key2")).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_CompletesNormally()
    {
        // Arrange — InMemoryTokenCacheProvider should honor CT without issue
        var cache = CreateCache();
        await cache.SetAsync("key", MakeToken());
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var result = await cache.GetAsync("key", cts.Token);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_FullAuthTokenRecord_PreservesAllFields()
    {
        // Arrange
        var cache = CreateCache();
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token = new AuthToken(
            AccessToken: "access-123",
            RefreshToken: "refresh-456",
            ExpiresAt: expiresAt,
            TokenType: "Bearer");

        // Act
        await cache.SetAsync("full-token-key", token);
        var result = await cache.GetAsync("full-token-key");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("access-123");
        result.RefreshToken.Should().Be("refresh-456");
        result.ExpiresAt.Should().BeCloseTo(expiresAt, precision: TimeSpan.FromMilliseconds(100));
        result.TokenType.Should().Be("Bearer");
    }
}
