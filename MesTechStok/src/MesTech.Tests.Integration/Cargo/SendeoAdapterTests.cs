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
/// SendeoCargoAdapter WireMock entegrasyon testleri.
/// Bearer token (API key) auth, JSON payloads, Polly retry + circuit breaker.
/// SupportsCancellation = false, SupportsMultiParcel = false.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "Sendeo")]
public class SendeoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<SendeoCargoAdapter> _logger;

    public SendeoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<SendeoCargoAdapter>();
    }

    private SendeoCargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new SendeoCargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["ApiKey"] = "test-sendeo-api-key",
            ["CustomerCode"] = "SND-CUST-001"
            // NO BaseUrl key — keeps WireMock BaseAddress
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "Sendeo Test Alici",
        RecipientPhone = "05554445566",
        RecipientAddress = new Address
        {
            Street = "Sendeo Test Cad 5",
            City = "Ankara",
            District = "Cankaya",
            PostalCode = "06100"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Bulvari 3",
            City = "Ankara",
            District = "Yenimahalle",
            PostalCode = "06170"
        },
        Weight = 2.0m,
        Desi = 6,
        ParcelCount = 1
    };

    // ══════════════════════════════════════
    // 1. CreateShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_Success_ReturnsTrackingNumber()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""SND-001"",""shipmentId"":""SNDSHIP-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("SND-001");
        result.ShipmentId.Should().Be("SNDSHIP-001");
        result.ErrorMessage.Should().BeNull();
        _mockServer.LogEntries.Should().HaveCount(1, "exactly one HTTP call expected for successful shipment");
    }

    // ══════════════════════════════════════
    // 2. CreateShipment_InvalidAddress
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_InvalidAddress_ReturnsFailedResult()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Alici adres gecersiz""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("BadRequest", "error message should include HTTP status name");
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 3. CreateShipment_ApiTimeout_PollyRetry
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ApiTimeout_PollyRetry_RetriesObserved()
    {
        // Arrange — 500 triggers Polly retry (MaxRetryAttempts = 3, so up to 4 total attempts)
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: failure with retries (1 initial + up to 3 retries = 4 max attempts)
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should retry at least once on 500");
    }

    // ══════════════════════════════════════
    // 4. CreateShipment_ServerError_CircuitBreaker
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_CircuitBreaker_ReturnsFailed()
    {
        // Arrange — persistent 503 triggers retries; circuit breaker may open after threshold
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CreateShipmentAsync(CreateTestRequest());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull("no tracking number on server error");
    }

    // ══════════════════════════════════════
    // 5. TrackShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_Success_ReturnsStatusAndEvents()
    {
        // Arrange
        const string trackingNo = "SND-001";
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
                            ""timestamp"": ""2026-03-15T08:00:00"",
                            ""location"": ""Sendeo Ankara Depo"",
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
        result.EstimatedDelivery.Should().NotBeNull("estimatedDelivery was provided in response");
        result.Events.Should().HaveCount(1);
        result.Events[0].Location.Should().Be("Sendeo Ankara Depo");
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
    }

    // ══════════════════════════════════════
    // 6. TrackShipment_NotFound
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_NotFound_ReturnsCreatedStatus()
    {
        // Arrange — 404 triggers graceful degradation (no exception)
        const string trackingNo = "SND-NOTFOUND";
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

        // Assert: graceful -- tracking number preserved, status defaults, no events
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.Created, "404 returns default Created status");
        result.Events.Should().BeEmpty("no events when shipment not found");
    }

    // ══════════════════════════════════════
    // 7. TrackShipment_MultipleEvents
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_MultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        const string trackingNo = "SND-MULTI";
        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""delivered"",
                    ""events"": [
                        {""timestamp"":""2026-03-15T08:00:00"",""location"":""Depo"",""description"":""Alindi"",""status"":""picked_up""},
                        {""timestamp"":""2026-03-15T16:00:00"",""location"":""Hub"",""description"":""Yolda"",""status"":""in_transit""},
                        {""timestamp"":""2026-03-16T11:00:00"",""location"":""Dagitim"",""description"":""Teslim Edildi"",""status"":""delivered""}
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.Delivered);
        result.Events.Should().HaveCount(3);
        result.Events[0].Location.Should().Be("Depo");
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
        result.Events[1].Status.Should().Be(CargoStatus.InTransit);
        result.Events[2].Status.Should().Be(CargoStatus.Delivered);
        result.Events[2].Description.Should().Be("Teslim Edildi");
    }

    // ══════════════════════════════════════
    // 8. CancelShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_Success_ReturnsFalse_NotSupported()
    {
        // Arrange — no WireMock stub needed; adapter returns false without HTTP call
        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("SNDSHIP-001");

        // Assert: Sendeo does not support cancellation
        result.Should().BeFalse("Sendeo does not support API cancellation");
        _mockServer.LogEntries.Should().BeEmpty("No HTTP call should be made for unsupported cancel");
        adapter.SupportsCancellation.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 9. CancelShipment_AlreadyDelivered
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_AlreadyDelivered_ReturnsFalse()
    {
        // Arrange — cancellation not supported regardless of shipment status
        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("SND-DELIVERED-001");

        // Assert
        result.Should().BeFalse("Sendeo cancellation not supported regardless of delivery status");
        _mockServer.LogEntries.Should().BeEmpty("No HTTP call should be made for unsupported cancel");
    }

    // ══════════════════════════════════════
    // 10. GetLabel_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_Success_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "SNDSHIP-001";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
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
        result.Data.Should().HaveCount(4, "PDF header is 4 bytes");
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        result.FileName.Should().Be($"sendeo-label-{shipmentId}.pdf");
    }

    // ══════════════════════════════════════
    // 11. GetLabel_NotReady
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_NotReady_ThrowsHttpRequestException()
    {
        // Arrange — 404 means label not yet generated
        const string shipmentId = "SND-NOTREADY";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}/label")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Etiket hazir degil""}"));

        var adapter = CreateConfiguredAdapter();

        // Act & Assert
        var act = async () => await adapter.GetShipmentLabelAsync(shipmentId);
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.WithMessage("*Sendeo label request failed*", "exception message should identify Sendeo label failure");
    }

    // ══════════════════════════════════════
    // 12. IsAvailable_Healthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Healthy_ReturnsTrue()
    {
        // Arrange
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
        _mockServer.LogEntries.Should().HaveCount(1, "health check should make exactly one HTTP call");
    }

    // ══════════════════════════════════════
    // 13. IsAvailable_Unhealthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Unhealthy_ReturnsFalse()
    {
        // Arrange — 503 from health endpoint means service is down
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/health").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
        _mockServer.LogEntries.Should().NotBeEmpty("at least one HTTP call should be attempted");
    }

    // ══════════════════════════════════════
    // 14. Auth_InvalidCredential
    // ══════════════════════════════════════

    [Fact]
    public async Task Auth_InvalidCredential_ReturnsFailedResult()
    {
        // Arrange — 401 is not a 5xx, so Polly should NOT retry
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"":""Gecersiz API anahtari""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: 401 treated as failure (not retriable — only 5xx triggers Polly)
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unauthorized", "error message should include HTTP status name");
        result.TrackingNumber.Should().BeNull();
        _mockServer.LogEntries.Should().HaveCount(1, "401 should not trigger Polly retry");
    }

    // ══════════════════════════════════════
    // 15. ConcurrentRequests_ThreadSafe
    // ══════════════════════════════════════

    [Fact]
    public async Task ConcurrentRequests_ThreadSafe_AllSucceed()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/v1/shipments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""SND-CONC"",""shipmentId"":""SNDSHIP-C""}"));

        var adapter = CreateConfiguredAdapter();

        // Act: fire 5 concurrent requests
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => adapter.CreateShipmentAsync(CreateTestRequest()))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert: all calls completed without deadlock or race conditions
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.Success, "all concurrent requests should succeed");
        results.Should().OnlyContain(r => r.TrackingNumber == "SND-CONC", "all should return same tracking number");
        _mockServer.LogEntries.Should().HaveCount(5, "WireMock should have received exactly 5 requests");
    }

    // ══════════════════════════════════════
    // 16. SupportsCancellation_ShouldBeFalse
    // ══════════════════════════════════════

    [Fact]
    public void SupportsCancellation_ShouldBeFalse()
    {
        // Sendeo does not support cancellation
        var adapter = CreateConfiguredAdapter();
        adapter.SupportsCancellation.Should().BeFalse("Sendeo does not expose a cancellation API endpoint");
    }

    // ══════════════════════════════════════
    // 17. SupportsMultiParcel_ShouldBeFalse
    // ══════════════════════════════════════

    [Fact]
    public void SupportsMultiParcel_ShouldBeFalse()
    {
        // Sendeo: multi-parcel not supported
        var adapter = CreateConfiguredAdapter();
        adapter.SupportsMultiParcel.Should().BeFalse("Sendeo does not support multi-parcel shipments");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
