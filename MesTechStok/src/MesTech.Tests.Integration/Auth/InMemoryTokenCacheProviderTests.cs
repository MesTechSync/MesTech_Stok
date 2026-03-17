using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Auth;

namespace MesTech.Tests.Integration.Auth;

/// <summary>
/// InMemoryTokenCacheProvider integration tests.
/// Tests cache hit, cache miss, cache expiry/removal, overwrite, and concurrent access.
/// DEV 3 Dalga 14+15.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "TokenCache")]
public class InMemoryTokenCacheProviderTests
{
  private InMemoryTokenCacheProvider CreateCache() => new();

  private static AuthToken CreateToken(
    string accessToken = "test-token",
    string? refreshToken = null,
    int expiresInMinutes = 60)
  {
    return new AuthToken(
      AccessToken: accessToken,
      RefreshToken: refreshToken,
      ExpiresAt: DateTime.UtcNow.AddMinutes(expiresInMinutes),
      TokenType: "Bearer");
  }

  // ════════════════════════════════════════════════════════════════
  //  1. Cache Hit — GetAsync returns stored token
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetAsync_ExistingKey_ReturnsStoredToken()
  {
    // Arrange
    var cache = CreateCache();
    var token = CreateToken(accessToken: "cached-abc123");
    await cache.SetAsync("oauth2:Amazon", token);

    // Act
    var result = await cache.GetAsync("oauth2:Amazon");

    // Assert
    result.Should().NotBeNull();
    result!.AccessToken.Should().Be("cached-abc123");
    result.TokenType.Should().Be("Bearer");
  }

  [Fact]
  public async Task GetAsync_DifferentKeys_ReturnCorrectTokens()
  {
    // Arrange
    var cache = CreateCache();
    var amazonToken = CreateToken(accessToken: "amazon-token");
    var ebayToken = CreateToken(accessToken: "ebay-token");

    await cache.SetAsync("oauth2:Amazon", amazonToken);
    await cache.SetAsync("oauth2:eBay", ebayToken);

    // Act
    var result1 = await cache.GetAsync("oauth2:Amazon");
    var result2 = await cache.GetAsync("oauth2:eBay");

    // Assert
    result1!.AccessToken.Should().Be("amazon-token");
    result2!.AccessToken.Should().Be("ebay-token");
  }

  [Fact]
  public async Task SetAsync_SameKeyTwice_OverwritesPreviousValue()
  {
    // Arrange
    var cache = CreateCache();
    var tokenOld = CreateToken(accessToken: "old-token");
    var tokenNew = CreateToken(accessToken: "new-token");

    await cache.SetAsync("oauth2:Test", tokenOld);
    await cache.SetAsync("oauth2:Test", tokenNew);

    // Act
    var result = await cache.GetAsync("oauth2:Test");

    // Assert
    result!.AccessToken.Should().Be("new-token");
  }

  // ════════════════════════════════════════════════════════════════
  //  2. Cache Miss — GetAsync returns null
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetAsync_NonExistentKey_ReturnsNull()
  {
    // Arrange
    var cache = CreateCache();

    // Act
    var result = await cache.GetAsync("oauth2:NonExistent");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetAsync_EmptyCache_ReturnsNull()
  {
    var cache = CreateCache();
    var result = await cache.GetAsync("any-key");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetAsync_AfterRemove_ReturnsNull()
  {
    // Arrange
    var cache = CreateCache();
    await cache.SetAsync("key", CreateToken());
    await cache.RemoveAsync("key");

    // Act
    var result = await cache.GetAsync("key");

    // Assert
    result.Should().BeNull();
  }

  // ════════════════════════════════════════════════════════════════
  //  3. Cache Expiry / Removal
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task RemoveAsync_ExistingKey_RemovesToken()
  {
    // Arrange
    var cache = CreateCache();
    await cache.SetAsync("oauth2:Remove", CreateToken(accessToken: "to-remove"));

    // Act
    await cache.RemoveAsync("oauth2:Remove");
    var result = await cache.GetAsync("oauth2:Remove");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
  {
    // Arrange
    var cache = CreateCache();

    // Act — should not throw
    var act = () => cache.RemoveAsync("non-existent-key");

    // Assert
    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task RemoveAsync_DoesNotAffectOtherKeys()
  {
    // Arrange
    var cache = CreateCache();
    await cache.SetAsync("key-a", CreateToken(accessToken: "token-a"));
    await cache.SetAsync("key-b", CreateToken(accessToken: "token-b"));

    // Act
    await cache.RemoveAsync("key-a");

    // Assert
    var resultA = await cache.GetAsync("key-a");
    var resultB = await cache.GetAsync("key-b");
    resultA.Should().BeNull();
    resultB.Should().NotBeNull();
    resultB!.AccessToken.Should().Be("token-b");
  }

  // ════════════════════════════════════════════════════════════════
  //  4. Concurrent Access — Thread Safety
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task ConcurrentSetAndGet_MultipleThreads_NoDataCorruption()
  {
    // Arrange
    var cache = CreateCache();
    var tasks = new List<Task>();
    var tokenCount = 100;

    // Act — concurrent writes
    for (var i = 0; i < tokenCount; i++)
    {
      var index = i;
      tasks.Add(Task.Run(async () =>
      {
        var token = CreateToken(accessToken: $"token-{index}");
        await cache.SetAsync($"key-{index}", token);
      }));
    }

    await Task.WhenAll(tasks);

    // Assert — all tokens accessible
    for (var i = 0; i < tokenCount; i++)
    {
      var result = await cache.GetAsync($"key-{i}");
      result.Should().NotBeNull($"key-{i} should exist");
      result!.AccessToken.Should().Be($"token-{i}");
    }
  }

  [Fact]
  public async Task ConcurrentReadWrite_SameKey_DoesNotThrow()
  {
    // Arrange
    var cache = CreateCache();
    await cache.SetAsync("shared-key", CreateToken(accessToken: "initial"));

    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var tasks = new List<Task>();

    // Act — concurrent reads and writes on the same key
    for (var i = 0; i < 50; i++)
    {
      var index = i;
      tasks.Add(Task.Run(async () =>
      {
        if (index % 2 == 0)
        {
          await cache.SetAsync("shared-key", CreateToken(accessToken: $"token-{index}"));
        }
        else
        {
          await cache.GetAsync("shared-key");
        }
      }));
    }

    // Assert — no exceptions
    var act = () => Task.WhenAll(tasks);
    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task ConcurrentRemoveAndGet_DoesNotThrow()
  {
    // Arrange
    var cache = CreateCache();
    for (var i = 0; i < 20; i++)
    {
      await cache.SetAsync($"key-{i}", CreateToken(accessToken: $"token-{i}"));
    }

    var tasks = new List<Task>();

    // Act — concurrent removes and gets
    for (var i = 0; i < 20; i++)
    {
      var index = i;
      tasks.Add(Task.Run(async () =>
      {
        await cache.RemoveAsync($"key-{index}");
      }));
      tasks.Add(Task.Run(async () =>
      {
        await cache.GetAsync($"key-{index}");
      }));
    }

    // Assert
    var act = () => Task.WhenAll(tasks);
    await act.Should().NotThrowAsync();
  }

  // ════════════════════════════════════════════════════════════════
  //  5. Token Properties Preserved
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task SetAsync_PreservesAllTokenProperties()
  {
    // Arrange
    var cache = CreateCache();
    var expiresAt = DateTime.UtcNow.AddHours(2);
    var token = new AuthToken(
      AccessToken: "full-token",
      RefreshToken: "refresh-value",
      ExpiresAt: expiresAt,
      TokenType: "CustomType");

    // Act
    await cache.SetAsync("full-key", token);
    var result = await cache.GetAsync("full-key");

    // Assert
    result.Should().NotBeNull();
    result!.AccessToken.Should().Be("full-token");
    result.RefreshToken.Should().Be("refresh-value");
    result.ExpiresAt.Should().Be(expiresAt);
    result.TokenType.Should().Be("CustomType");
  }

  [Fact]
  public async Task SetAsync_NullRefreshToken_PreservesNull()
  {
    // Arrange
    var cache = CreateCache();
    var token = new AuthToken(
      AccessToken: "no-refresh",
      RefreshToken: null,
      ExpiresAt: DateTime.UtcNow.AddHours(1),
      TokenType: "Bearer");

    // Act
    await cache.SetAsync("null-refresh-key", token);
    var result = await cache.GetAsync("null-refresh-key");

    // Assert
    result!.RefreshToken.Should().BeNull();
  }

  // ════════════════════════════════════════════════════════════════
  //  6. CancellationToken Support
  // ════════════════════════════════════════════════════════════════

  [Fact]
  public async Task GetAsync_WithCancellationToken_CompletesSuccessfully()
  {
    var cache = CreateCache();
    await cache.SetAsync("ct-key", CreateToken(), CancellationToken.None);

    using var cts = new CancellationTokenSource();
    var result = await cache.GetAsync("ct-key", cts.Token);

    result.Should().NotBeNull();
  }

  [Fact]
  public async Task SetAsync_WithCancellationToken_CompletesSuccessfully()
  {
    var cache = CreateCache();
    using var cts = new CancellationTokenSource();

    var act = () => cache.SetAsync("ct-set-key", CreateToken(), cts.Token);
    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task RemoveAsync_WithCancellationToken_CompletesSuccessfully()
  {
    var cache = CreateCache();
    await cache.SetAsync("ct-remove-key", CreateToken());

    using var cts = new CancellationTokenSource();
    var act = () => cache.RemoveAsync("ct-remove-key", cts.Token);
    await act.Should().NotThrowAsync();
  }
}
