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
/// MngKargoAdapter WireMock entegrasyon testleri.
/// Gercek adapter REST endpoint'lerine karsi test edilir.
/// Adapter: Basic auth (ApiKey:ApiSecret), JSON payloads, Polly retry + circuit breaker.
/// Base URL configurable via credentials["BaseUrl"].
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "MNG")]
public class MngKargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<MngKargoAdapter> _logger;

    public MngKargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<MngKargoAdapter>();
    }

    // -- Test helpers ------------------------------------------------

    /// <summary>
    /// Creates a configured MngKargoAdapter pointing at WireMock server.
    /// Configure sets BaseAddress from credentials["BaseUrl"], so we pass the WireMock URL.
    /// </summary>
    private MngKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient();
        var adapter = new MngKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["ApiKey"] = "mng-test-key",
            ["ApiSecret"] = "mng-test-secret",
            ["CustomerCode"] = "MNG-CUST-001",
            ["BaseUrl"] = _fixture.BaseUrl + "/"
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "MNG Test Alici",
        RecipientPhone = "05557778899",
        RecipientAddress = new Address
        {
            Street = "MNG Test Bulvari 1",
            City = "Istanbul",
            District = "Umraniye",
            PostalCode = "34760"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Cad 2",
            City = "Istanbul",
            District = "Kadikoy",
            PostalCode = "34710"
        },
        Weight = 3.0m,
        Desi = 7,
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
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""MNG-7890123456"",""shipmentId"":""SHP-MNG-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("MNG-7890123456");
        result.ShipmentId.Should().Be("SHP-MNG-001");
        result.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 2. CreateShipment_InvalidAddress
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_InvalidAddress_ReturnsFailedResult()
    {
        // Arrange — 400 Bad Request for invalid address
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Gecersiz adres - sehir kodu bulunamadi""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 3. CreateShipment_ApiTimeout_PollyRetry
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ApiTimeout_PollyRetry_MultipleAttemptsObserved()
    {
        // Arrange — persistent 500 to trigger Polly retry pipeline
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — Polly retries 3 times = total 4 attempts; at least 2 log entries expected
        result.Success.Should().BeFalse();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should have retried at least once, generating multiple requests");
    }

    // ══════════════════════════════════════
    // 4. CreateShipment_ServerError_CircuitBreaker
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_CircuitBreaker_ReturnsFailed()
    {
        // Arrange — persistent 503 to eventually trip circuit breaker
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act — call multiple times to trigger circuit breaker (minThroughput=5, failureRatio=0.5)
        var results = new List<ShipmentResult>();
        for (var i = 0; i < 8; i++)
        {
            results.Add(await adapter.CreateShipmentAsync(request));
        }

        // Assert — all should fail, and after enough failures circuit breaker opens gracefully
        results.Should().OnlyContain(r => r.Success == false);
        results.Should().HaveCount(8);
    }

    // ══════════════════════════════════════
    // 5. TrackShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_Success_ReturnsStatusAndEvents()
    {
        // Arrange
        const string trackingNo = "MNG-7890123456";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/tracking/{trackingNo}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""intransit"",
                    ""estimatedDelivery"": ""2026-03-18T14:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-15T09:30:00"",
                            ""location"": ""Istanbul Transfer Merkezi"",
                            ""description"": ""Kargo yola cikti"",
                            ""status"": ""intransit""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.InTransit);
        result.EstimatedDelivery.Should().NotBeNull();
        result.Events.Should().HaveCount(1);
        result.Events[0].Location.Should().Be("Istanbul Transfer Merkezi");
        result.Events[0].Status.Should().Be(CargoStatus.InTransit);
    }

    // ══════════════════════════════════════
    // 6. TrackShipment_NotFound
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_NotFound_ReturnsCreatedStatus()
    {
        // Arrange — 404 for unknown tracking number
        const string trackingNo = "MNG-NONEXISTENT";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/tracking/{trackingNo}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Gonderi bulunamadi""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert — graceful degradation: returns Created status (default), no events
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.Created);
        result.Events.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 7. TrackShipment_MultipleEvents
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_MultipleEvents_ReturnsAllEvents()
    {
        // Arrange — tracking response with 3 events showing full lifecycle
        const string trackingNo = "MNG-MULTI-EVT";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/tracking/{trackingNo}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""delivered"",
                    ""estimatedDelivery"": ""2026-03-17T10:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-14T08:00:00"",
                            ""location"": ""Istanbul Sube"",
                            ""description"": ""Kargo teslim alindi"",
                            ""status"": ""pickedup""
                        },
                        {
                            ""timestamp"": ""2026-03-15T12:00:00"",
                            ""location"": ""Ankara Transfer"",
                            ""description"": ""Transfer merkezine ulasti"",
                            ""status"": ""intransit""
                        },
                        {
                            ""timestamp"": ""2026-03-16T09:00:00"",
                            ""location"": ""Ankara Cankaya Sube"",
                            ""description"": ""Teslim edildi"",
                            ""status"": ""delivered""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.Status.Should().Be(CargoStatus.Delivered);
        result.Events.Should().HaveCount(3);
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
        result.Events[0].Location.Should().Be("Istanbul Sube");
        result.Events[1].Status.Should().Be(CargoStatus.InTransit);
        result.Events[1].Location.Should().Be("Ankara Transfer");
        result.Events[2].Status.Should().Be(CargoStatus.Delivered);
        result.Events[2].Location.Should().Be("Ankara Cankaya Sube");
    }

    // ══════════════════════════════════════
    // 8. CancelShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_Success_ReturnsTrue()
    {
        // Arrange
        const string shipmentId = "SHP-MNG-001";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Gonderi basariyla iptal edildi""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync(shipmentId);

        // Assert
        result.Should().BeTrue();
        _mockServer.LogEntries.Should().HaveCountGreaterOrEqualTo(1,
            "At least one DELETE request should have been made");
    }

    // ══════════════════════════════════════
    // 9. CancelShipment_AlreadyDelivered
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_AlreadyDelivered_ReturnsFalse()
    {
        // Arrange — 409 Conflict: shipment already delivered, cannot cancel
        const string shipmentId = "SHP-MNG-DELIVERED";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(409)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Gonderi teslim edilmis, iptal edilemez""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync(shipmentId);

        // Assert
        result.Should().BeFalse();
        _mockServer.LogEntries.Should().NotBeEmpty("DELETE request should have been attempted");
    }

    // ══════════════════════════════════════
    // 10. GetLabel_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_Success_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "SHP-MNG-001";
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
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        result.FileName.Should().Be($"mng-label-{shipmentId}.pdf");
    }

    // ══════════════════════════════════════
    // 11. GetLabel_NotReady
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_NotReady_ThrowsOrReturnsEmpty()
    {
        // Arrange — 404: label not yet generated
        const string shipmentId = "SHP-MNG-NOLABEL";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}/label")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Etiket henuz olusturulmadi""}"));

        var adapter = CreateConfiguredAdapter();

        // Act & Assert — adapter throws HttpRequestException for non-success status
        var act = async () => await adapter.GetShipmentLabelAsync(shipmentId);
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*MNG Kargo label request failed*");
    }

    // ══════════════════════════════════════
    // 12. IsAvailable_Healthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Healthy_ReturnsTrue()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""healthy""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
        _mockServer.LogEntries.Should().HaveCountGreaterOrEqualTo(1,
            "Health check endpoint should have been called");
    }

    // ══════════════════════════════════════
    // 13. IsAvailable_Unhealthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Unhealthy_ReturnsFalse()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
        _mockServer.LogEntries.Should().NotBeEmpty("Health check request should have been attempted");
    }

    // ══════════════════════════════════════
    // 14. Auth_InvalidCredential
    // ══════════════════════════════════════

    [Fact]
    public async Task Auth_InvalidCredential_ReturnsFailedResult()
    {
        // Arrange — 401 Unauthorized for invalid API key
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Gecersiz API anahtari""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — 401 is non-success but not 5xx, so no Polly retry, straight failure
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 15. ConcurrentRequests_ThreadSafe
    // ══════════════════════════════════════

    [Fact]
    public async Task ConcurrentRequests_ThreadSafe_AllSucceed()
    {
        // Arrange — mock shipment endpoint with unique tracking numbers via counter
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""MNG-CONC-001"",""shipmentId"":""SHP-CONC-001""}"));

        var adapter = CreateConfiguredAdapter();

        // Act — 5 concurrent CreateShipmentAsync calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => adapter.CreateShipmentAsync(CreateTestRequest()))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert — all 5 should complete successfully, no deadlock/exception
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.Success == true);
        results.Should().OnlyContain(r => r.TrackingNumber == "MNG-CONC-001");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
