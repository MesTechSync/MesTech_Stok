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
/// PazaramaAdapter unit tests — OAuth2 marketplace adapter.
/// G487: TestConnection (OAuth token), PullProducts, properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "Pazarama")]
public class PazaramaAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<PazaramaAdapter>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

    private PazaramaAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://isortagim.pazarama.com/")
        };
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_handler));

        return new PazaramaAdapter(httpClient, _loggerMock.Object, _httpClientFactoryMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ClientId"] = "pzr-client-id",
        ["ClientSecret"] = "pzr-client-secret",
        ["BaseUrl"] = "https://isortagim.pazarama.com/"
    };

    // ─── Properties ───

    [Fact]
    public void PlatformCode_ReturnsPazarama()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.Pazarama));
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
    public async Task TestConnectionAsync_ValidOAuth_ReturnsSuccess()
    {
        // Arrange — enqueue OAuth token response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"access_token":"eyJhbGciOiJSUzI1NiJ9.test","expires_in":3600,"token_type":"Bearer"}""");

        // Then enqueue products response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"data":{"items":[],"totalCount":0},"isSuccess":true}""");

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
        // Arrange — OAuth returns 401
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized,
            """{"error":"invalid_client","error_description":"Client not found"}""");

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

    // ─── PullProducts ───

    [Fact]
    public async Task PullProductsAsync_NotAuthenticated_ThrowsOrReturnsEmpty()
    {
        var adapter = CreateAdapter();

        // Act & Assert — not authenticated
        var action = async () => await adapter.PullProductsAsync();
        await action.Should().ThrowAsync<Exception>();
    }
}
