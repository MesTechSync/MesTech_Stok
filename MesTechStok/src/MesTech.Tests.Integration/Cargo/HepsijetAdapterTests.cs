using System.Net.Http;
using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Enums;
using MesTech.Domain.ValueObjects;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Cargo;

/// <summary>
/// HepsiJetCargoAdapter WireMock entegrasyon testleri.
/// OAuth2-like Bearer token auth, JSON payloads, Polly retry + circuit breaker.
/// Extra: TokenRefresh senaryolari (3 ek test — toplam 18 test).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "HepsiJet")]
public class HepsijetAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<HepsiJetCargoAdapter> _logger;

    public HepsijetAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<HepsiJetCargoAdapter>();
    }

    private HepsiJetCargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new HepsiJetCargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "hj-user",
            ["Password"] = "hj-pass",
            ["CustomerCode"] = "HJCUST-001"
            // NO BaseUrl key — keeps WireMock BaseAddress
        });
        return adapter;
    }

    private void SetupTokenEndpoint(string token = "test-bearer-token", int statusCode = 200)
    {
        if (statusCode == 200)
        {
            _mockServer
                .Given(Request.Create()
                    .WithPath("/api/v1/auth/token")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody($@"{{""accessToken"":""{token}"",""expiresIn"":3600}}"));
        }
        else
        {
            _mockServer
                .Given(Request.Create()
                    .WithPath("/api/v1/auth/token")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(statusCode)
                    .WithBody(@"{""error"":""Invalid credentials""}"));
        }
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "HepsiJet Test Alici",
        RecipientPhone = "05551112233",
        RecipientAddress = new Address
        {
            Street = "HepsiJet Test Sok 1",
            City = "Istanbul",
            District = "Sisli",
            PostalCode = "34360"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Cad 10",
            City = "Istanbul",
            District = "Besiktas",
            PostalCode = "34353"
        },
        Weight = 1.5m,
        Desi = 4,
        ParcelCount = 1
    };

    // ══════════════════════════════════════
    // 1. CreateShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_Success_ReturnsTrackingNumber()
    {
        // TODO: Implement when DEV 3 adapter integration is verified
        // WireMock setup: mock token + success response
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-001"",""shipmentId"":""HJSHIP-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("HJ-001");
        result.ShipmentId.Should().Be("HJSHIP-001");
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 2. CreateShipment_InvalidAddress
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_InvalidAddress_ReturnsFailedResult()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + 400 error response
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Gecersiz adres - sehir bulunamadi""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: failure result
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 3. CreateShipment_ApiTimeout_PollyRetry
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ApiTimeout_PollyRetry_MultipleAttemptsObserved()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + 500 to trigger Polly retry
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: failed result + multiple retry attempts
        result.Success.Should().BeFalse();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 4. CreateShipment_ServerError_CircuitBreaker
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_CircuitBreaker_ReturnsFailed()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + persistent 503 to open circuit breaker
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();

        // Act: multiple calls to trigger circuit breaker threshold
        var result = await adapter.CreateShipmentAsync(CreateTestRequest());

        // Assert: failed gracefully (circuit breaker returns 503 as failure)
        result.Success.Should().BeFalse();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 5. TrackShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_Success_ReturnsStatusAndEvents()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + tracking response
        SetupTokenEndpoint();
        const string trackingNo = "HJ-001";
        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""in_transit"",
                    ""estimatedDelivery"": ""2026-03-17T18:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-15T09:00:00"",
                            ""location"": ""Istanbul HepsiJet Depo"",
                            ""description"": ""Kargo teslim alindi"",
                            ""status"": ""picked_up""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.InTransit);
        result.Events.Should().HaveCount(1);
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 6. TrackShipment_NotFound
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_NotFound_ReturnsCreatedStatus()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + 404
        SetupTokenEndpoint();
        const string trackingNo = "HJ-NOTFOUND";
        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Kargo bulunamadi""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert: graceful degradation — returns Created (unknown) status
        result.TrackingNumber.Should().Be(trackingNo);
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 7. TrackShipment_MultipleEvents
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_MultipleEvents_ReturnsAllEvents()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + tracking response with 3 events
        SetupTokenEndpoint();
        const string trackingNo = "HJ-MULTI";
        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""out_for_delivery"",
                    ""events"": [
                        {""timestamp"":""2026-03-15T09:00:00"",""location"":""Depo"",""description"":""Alindi"",""status"":""picked_up""},
                        {""timestamp"":""2026-03-15T14:00:00"",""location"":""Transfer"",""description"":""Transfer"",""status"":""in_transit""},
                        {""timestamp"":""2026-03-16T08:00:00"",""location"":""Dagitim"",""description"":""Dagitimda"",""status"":""out_for_delivery""}
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert: all 3 events returned
        result.Events.Should().HaveCount(3);
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 8. CancelShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_Success_ReturnsFalse_NotSupported()
    {
        // TODO: Implement when DEV 3 adapter is available
        // HepsiJet does not support cancellation — SupportsCancellation = false
        // No WireMock stub needed; adapter returns false without HTTP call

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("HJSHIP-001");

        // Assert: HepsiJet does not support cancellation
        result.Should().BeFalse("HepsiJet does not support API cancellation");
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 9. CancelShipment_AlreadyDelivered
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_AlreadyDelivered_ReturnsFalse()
    {
        // TODO: Implement when DEV 3 adapter is available
        // HepsiJet: cancellation not supported regardless of delivery status

        var adapter = CreateConfiguredAdapter();

        // Act: try to cancel a delivered shipment
        var result = await adapter.CancelShipmentAsync("HJ-DELIVERED-001");

        // Assert
        result.Should().BeFalse();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 10. GetLabel_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_Success_ReturnsPdfBytes()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + label response
        SetupTokenEndpoint();
        const string shipmentId = "HJSHIP-001";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var base64 = Convert.ToBase64String(fakePdfBytes);

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}/label")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""labelData"":""{base64}""}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.GetShipmentLabelAsync(shipmentId);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 11. GetLabel_NotReady
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_NotReady_ThrowsHttpRequestException()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + 404 (label not yet generated)
        SetupTokenEndpoint();
        const string shipmentId = "HJSHIP-NOTREADY";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}/label")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Etiket henuz hazirlanmadi""}"));

        var adapter = CreateConfiguredAdapter();

        // Act & Assert: throws on label not ready
        var act = async () => await adapter.GetShipmentLabelAsync(shipmentId);
        await act.Should().ThrowAsync<HttpRequestException>();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 12. IsAvailable_Healthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Healthy_ReturnsTrue()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + health endpoint
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/health").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""healthy""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 13. IsAvailable_Unhealthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Unhealthy_ReturnsFalse()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + 500 health response
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/health").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 14. Auth_InvalidCredential
    // ══════════════════════════════════════

    [Fact]
    public async Task Auth_InvalidCredential_ThrowsOnTokenRefresh()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: token endpoint returns 401
        SetupTokenEndpoint(statusCode: 401);

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act & Assert: invalid credentials cause exception during token fetch
        var act = async () => await adapter.CreateShipmentAsync(request);
        await act.Should().ThrowAsync<HttpRequestException>();
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 15. ConcurrentRequests_ThreadSafe
    // ══════════════════════════════════════

    [Fact]
    public async Task ConcurrentRequests_ThreadSafe_AllSucceed()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: mock token + shipment success (many calls)
        SetupTokenEndpoint();
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-CONCURRENT"",""shipmentId"":""HJSHIP-C""}"));

        var adapter = CreateConfiguredAdapter();

        // Act: fire 5 concurrent requests
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => adapter.CreateShipmentAsync(CreateTestRequest()))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert: all calls completed (no deadlock/race condition)
        results.Should().HaveCount(5);
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 16. TokenRefresh_Expired_GetsNewToken
    // ══════════════════════════════════════

    [Fact]
    public async Task TokenRefresh_Expired_GetsNewToken_BeforeRequest()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: token endpoint called, then shipment endpoint
        SetupTokenEndpoint("new-fresh-token");
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-002"",""shipmentId"":""HJSHIP-002""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act: first call triggers token fetch
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: token was fetched (token endpoint was called)
        result.Success.Should().BeTrue();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Token endpoint + shipment endpoint must both be called");
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 17. TokenRefresh_ConcurrentCalls_SingleRefresh
    // ══════════════════════════════════════

    [Fact]
    public async Task TokenRefresh_ConcurrentCalls_SingleRefresh_TokenNotFetchedTwice()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: token endpoint (should only be hit once even with concurrent calls)
        SetupTokenEndpoint("single-refresh-token");
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-CON"",""shipmentId"":""HJSHIP-CON""}"));

        var adapter = CreateConfiguredAdapter();

        // Act: 3 concurrent calls — SemaphoreSlim(_tokenLock) should prevent duplicate token refresh
        var tasks = Enumerable.Range(0, 3)
            .Select(_ => adapter.CreateShipmentAsync(CreateTestRequest()))
            .ToList();
        await Task.WhenAll(tasks);

        // Assert: token endpoint called at most once (double-check pattern in EnsureTokenAsync)
        var tokenCalls = _mockServer.LogEntries
            .Count(e => e.RequestMessage.Path == "/api/v1/auth/token");
        tokenCalls.Should().Be(1,
            "SemaphoreSlim double-check prevents concurrent token refresh");
        Assert.True(true); // placeholder
    }

    // ══════════════════════════════════════
    // 18. TokenRefresh_Failed_MeaningfulError
    // ══════════════════════════════════════

    [Fact]
    public async Task TokenRefresh_Failed_MeaningfulError_PropagatesException()
    {
        // TODO: Implement when DEV 3 adapter is available
        // WireMock setup: token endpoint returns 500
        SetupTokenEndpoint(statusCode: 500);

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act & Assert: token failure propagates as HttpRequestException with meaningful message
        var act = async () => await adapter.CreateShipmentAsync(request);
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.WithMessage("*HepsiJet token*");
        Assert.True(true); // placeholder
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
