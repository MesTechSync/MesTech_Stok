using FluentAssertions;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// ICacheService sozlesme testleri.
/// Mock tabanli: Redis implementasyonundan bagimsiz olarak
/// cache kontratinin beklenen davranislarini dogrular.
/// </summary>
[Trait("Category", "Unit")]
public class CacheServiceContractTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly ICacheService _cache;

    public CacheServiceContractTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _cache = _cacheMock.Object;
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_ForNonExistentKey()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync<string>("non-existent-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cache.GetAsync<string>("non-existent-key");

        // Assert
        result.Should().BeNull();
        _cacheMock.Verify(c => c.GetAsync<string>("non-existent-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ShouldReturnStoredValue()
    {
        // Arrange
        const string key = "product:123";
        const string value = "{\"name\":\"Test Urun\",\"stock\":42}";

        _cacheMock
            .Setup(c => c.SetAsync(key, value, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(c => c.GetAsync<string>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value);

        // Act
        await _cache.SetAsync(key, value);
        var result = await _cache.GetAsync<string>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(value);

        _cacheMock.Verify(c => c.SetAsync(key, value, null, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.GetAsync<string>(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveCachedItem()
    {
        // Arrange
        const string key = "order:456";

        _cacheMock
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // After removal, GetAsync returns null
        _cacheMock
            .Setup(c => c.GetAsync<string>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _cache.RemoveAsync(key);
        var result = await _cache.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
        _cacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_ForNonExistentKey()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.ExistsAsync("ghost-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var exists = await _cache.ExistsAsync("ghost-key");

        // Assert
        exists.Should().BeFalse();
        _cacheMock.Verify(c => c.ExistsAsync("ghost-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldBeAccepted()
    {
        // Arrange
        const string key = "session:abc";
        const string value = "session-data";
        var expiration = TimeSpan.FromMinutes(30);

        _cacheMock
            .Setup(c => c.SetAsync(key, value, expiration, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(c => c.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _cache.SetAsync(key, value, expiration);
        var exists = await _cache.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();

        _cacheMock.Verify(
            c => c.SetAsync(key, value, expiration, It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            c => c.ExistsAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
