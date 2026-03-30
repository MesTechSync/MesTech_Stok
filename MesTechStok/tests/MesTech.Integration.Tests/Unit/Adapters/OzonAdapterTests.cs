using System.Net;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// OzonAdapter unit tests — Russian marketplace REST adapter.
/// G487: TestConnection, PullProducts, properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "Ozon")]
public class OzonAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<OzonAdapter>> _loggerMock = new();

    private OzonAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api-seller.ozon.ru/")
        };
        return new OzonAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ClientId"] = "ozon-client-id",
        ["ApiKey"] = "ozon-api-key",
        ["BaseUrl"] = "https://api-seller.ozon.ru/"
    };

    // ─── Properties ───

    [Fact]
    public void PlatformCode_ReturnsOzon()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.Ozon));
    }

    [Fact]
    public void SupportsStockUpdate_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsPriceUpdate_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }

    // ─── TestConnection ───

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange — Ozon product list response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"result":{"items":[],"total":0,"last_id":""},"has_next":false}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidCredentials_ReturnsFailed()
    {
        // Arrange — 403 Forbidden (wrong API key)
        _handler.EnqueueResponse(HttpStatusCode.Forbidden,
            """{"code":16,"message":"Client-Id or Api-Key is invalid"}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_EmptyCredentials_ReturnsFailed()
    {
        var adapter = CreateAdapter();
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        result.IsSuccess.Should().BeFalse();
    }

    // ─── Header Verification ───

    [Fact]
    public async Task TestConnectionAsync_SetsOzonHeaders()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"result":{"items":[],"total":0,"last_id":""},"has_next":false}""");

        var adapter = CreateAdapter();

        // Act
        await adapter.TestConnectionAsync(ValidCredentials());

        // Assert — check Client-Id header
        _handler.CapturedRequests.Should().NotBeEmpty();
        var request = _handler.CapturedRequests[0];
        request.Headers.Should().Contain(h => h.Key == "Client-Id");
        request.Headers.Should().Contain(h => h.Key == "Api-Key");
    }

    // ─── PullProducts ───

    [Fact]
    public async Task PullProductsAsync_NotConfigured_ThrowsOrReturnsEmpty()
    {
        var adapter = CreateAdapter();

        // Act & Assert — adapter not authenticated
        var action = async () => await adapter.PullProductsAsync();
        await action.Should().ThrowAsync<Exception>();
    }
}
