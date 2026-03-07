using FluentAssertions;
using MesTech.Tests.Integration.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using System.Text;

namespace MesTech.Tests.Integration.Cache;

/// <summary>
/// Redis integration tests via Testcontainers.
/// Tests IDistributedCache set/get/remove/expiration against real Redis 7.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class RedisCacheTests : IClassFixture<RedisContainerFixture>
{
    private readonly RedisContainerFixture _fixture;

    public RedisCacheTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private IDistributedCache CreateCache()
    {
        var options = Options.Create(new RedisCacheOptions
        {
            Configuration = _fixture.ConnectionString
        });
        return new RedisCache(options);
    }

    [Fact]
    public async Task SetAndGet_ShouldRoundtrip()
    {
        var cache = CreateCache();
        var key = $"test:{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("MesTech Redis Test");

        await cache.SetAsync(key, value);
        var result = await cache.GetAsync(key);

        result.Should().NotBeNull();
        Encoding.UTF8.GetString(result!).Should().Be("MesTech Redis Test");
    }

    [Fact]
    public async Task Get_NonExistentKey_ShouldReturnNull()
    {
        var cache = CreateCache();

        var result = await cache.GetAsync("nonexistent:key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ShouldDeleteKey()
    {
        var cache = CreateCache();
        var key = $"test:remove:{Guid.NewGuid()}";
        await cache.SetAsync(key, Encoding.UTF8.GetBytes("to be deleted"));

        await cache.RemoveAsync(key);
        var result = await cache.GetAsync(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetWithExpiration_ShouldExpire()
    {
        var cache = CreateCache();
        var key = $"test:expiry:{Guid.NewGuid()}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
        };

        await cache.SetAsync(key, Encoding.UTF8.GetBytes("expiring"), options);

        // Should exist immediately
        var immediate = await cache.GetAsync(key);
        immediate.Should().NotBeNull();

        // Wait for expiration
        await Task.Delay(1500);
        var expired = await cache.GetAsync(key);
        expired.Should().BeNull("key should have expired after 1 second");
    }
}
