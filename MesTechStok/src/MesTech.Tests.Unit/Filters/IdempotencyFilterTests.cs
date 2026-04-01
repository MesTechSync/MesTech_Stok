using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.WebApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Filters;

/// <summary>
/// IdempotencyFilter unit tests — G027: cache hit/miss, TTL, graceful degradation, header validation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Idempotency")]
public class IdempotencyFilterTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<IdempotencyFilter>> _loggerMock = new();

    private (EndpointFilterInvocationContext context, DefaultHttpContext httpContext) CreateContext(
        string method = "POST",
        string? idempotencyKey = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;

        if (idempotencyKey is not null)
            httpContext.Request.Headers["X-Idempotency-Key"] = idempotencyKey;

        var services = new ServiceCollection();
        services.AddSingleton(_cacheMock.Object);
        services.AddSingleton(_loggerMock.Object);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Capture response body
        httpContext.Response.Body = new MemoryStream();

        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        return (context, httpContext);
    }

    // Test 1: GET request bypasses filter (no idempotency for reads)
    [Fact]
    public async Task GetRequest_BypassesFilter_CallsNext()
    {
        var filter = new IdempotencyFilter();
        var (context, _) = CreateContext("GET", "test-key");
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("data"));
        });

        nextCalled.Should().BeTrue();
    }

    // Test 2: POST without header calls next normally
    [Fact]
    public async Task PostWithoutHeader_CallsNext_NoCaching()
    {
        var filter = new IdempotencyFilter();
        var (context, _) = CreateContext("POST");
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("created"));
        });

        nextCalled.Should().BeTrue();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Test 3: First POST with key — cache miss, calls next, caches result
    [Fact]
    public async Task FirstPostWithKey_CacheMiss_CallsNextAndCaches()
    {
        var filter = new IdempotencyFilter();
        var key = Guid.NewGuid().ToString();
        var (context, _) = CreateContext("POST", key);

        _cacheMock
            .Setup(c => c.GetAsync($"idempotency:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var nextCalled = false;
        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("created"));
        });

        nextCalled.Should().BeTrue();
        _cacheMock.Verify(c => c.SetAsync(
            $"idempotency:{key}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 4: Second POST with same key — cache hit, returns cached, skips handler
    [Fact]
    public async Task SecondPostWithKey_CacheHit_ReturnsCachedResponse()
    {
        var filter = new IdempotencyFilter();
        var key = "order-123";
        var (context, httpContext) = CreateContext("POST", key);

        // Must use CamelCase to match the filter's JsonOptions used for deserialization
        var cacheJsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var cachedEntry = JsonSerializer.Serialize(new { StatusCode = 201, Body = "{\"id\":\"abc\"}" }, cacheJsonOptions);
        _cacheMock
            .Setup(c => c.GetAsync($"idempotency:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedEntry));

        var nextCalled = false;
        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.Should().BeFalse("handler should NOT be called on cache hit");
        httpContext.Response.StatusCode.Should().Be(201);
        httpContext.Response.Headers["X-Idempotency-Replayed"].ToString().Should().Be("true");
    }

    // Test 5: Cache failure on write — request still succeeds (graceful degradation)
    [Fact]
    public async Task CacheWriteFailure_RequestStillSucceeds()
    {
        var filter = new IdempotencyFilter();
        var key = "order-456";
        var (context, _) = CreateContext("POST", key);

        _cacheMock
            .Setup(c => c.GetAsync($"idempotency:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis down"));

        var nextCalled = false;
        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Created("/api/v1/orders/1", new { id = 1 }));
        });

        nextCalled.Should().BeTrue("handler should still execute even if cache write fails");
    }

    // Test 6: PUT method also triggers idempotency
    [Fact]
    public async Task PutWithKey_TriggersIdempotencyCheck()
    {
        var filter = new IdempotencyFilter();
        var key = "update-789";
        var (context, _) = CreateContext("PUT", key);

        _cacheMock
            .Setup(c => c.GetAsync($"idempotency:{key}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        await filter.InvokeAsync(context, _ =>
            ValueTask.FromResult<object?>(Results.NoContent()));

        _cacheMock.Verify(c => c.GetAsync($"idempotency:{key}", It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 7: Empty idempotency key header treated as absent
    [Fact]
    public async Task EmptyKeyHeader_BypassesFilter()
    {
        var filter = new IdempotencyFilter();
        var (context, _) = CreateContext("POST", "");
        var nextCalled = false;

        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.Should().BeTrue();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
