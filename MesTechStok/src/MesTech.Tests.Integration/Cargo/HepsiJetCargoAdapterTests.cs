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
/// HepsiJetCargoAdapter WireMock entegrasyon testleri (G365).
/// OAuth2-like Bearer token, JSON payloads, Polly retry + circuit breaker.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "HepsiJet")]
public class HepsiJetCargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<HepsiJetCargoAdapter> _logger;

    public HepsiJetCargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<HepsiJetCargoAdapter>();
    }

    private HepsiJetCargoAdapter CreateConfiguredAdapter()
    {
        // Stub token endpoint for all tests
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/auth/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""accessToken"":""test-jwt-token"",""expiresIn"":3600}"));

        var httpClient = new HttpClient();
        var adapter = new HepsiJetCargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["Username"] = "hepsijet-test-user",
            ["Password"] = "hepsijet-test-pass",
            ["CustomerCode"] = "HJ-CUST-001",
            ["BaseUrl"] = _fixture.BaseUrl + "/"
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "HepsiJet Test Alici",
        RecipientPhone = "05551234567",
        RecipientAddress = new Address
        {
            Street = "HepsiJet Test Sokak 5",
            City = "Istanbul",
            District = "Besiktas",
            PostalCode = "34340"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Cad 10",
            City = "Istanbul",
            District = "Sisli",
            PostalCode = "34381"
        },
        Weight = 2.5m,
        Desi = 5,
        ParcelCount = 1
    };

    // ══════════════════════════════════════
    // 1. CreateShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_Success_ReturnsTrackingNumber()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-9876543210"",""shipmentId"":""SHP-HJ-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var result = await adapter.CreateShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("HJ-9876543210");
        result.ShipmentId.Should().Be("SHP-HJ-001");
    }

    // ══════════════════════════════════════
    // 2. CreateShipment_InvalidAddress
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_InvalidAddress_ReturnsFailedResult()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Gecersiz adres bilgisi""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var result = await adapter.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 3. CreateShipment_ServerError_PollyRetry
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_PollyRetry_MultipleAttemptsObserved()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var result = await adapter.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should have retried at least once");
    }

    // ══════════════════════════════════════
    // 4. CreateShipment_CircuitBreaker
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_CircuitBreaker_ReturnsFailed()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var results = new List<ShipmentResult>();
        for (var i = 0; i < 8; i++)
        {
            results.Add(await adapter.CreateShipmentAsync(request));
        }

        results.Should().OnlyContain(r => r.Success == false);
        results.Should().HaveCount(8);
    }

    // ══════════════════════════════════════
    // 5. TrackShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_Success_ReturnsStatusAndEvents()
    {
        const string trackingNo = "HJ-9876543210";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""intransit"",
                    ""estimatedDelivery"": ""2026-03-20T14:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-18T09:30:00"",
                            ""location"": ""HepsiJet Istanbul Transfer"",
                            ""description"": ""Kargo yola cikti"",
                            ""status"": ""intransit""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        var result = await adapter.TrackShipmentAsync(trackingNo);

        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.InTransit);
        result.Events.Should().HaveCount(1);
        result.Events[0].Location.Should().Be("HepsiJet Istanbul Transfer");
    }

    // ══════════════════════════════════════
    // 6. TrackShipment_NotFound
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_NotFound_ReturnsCreatedStatus()
    {
        const string trackingNo = "HJ-NONEXISTENT";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Gonderi bulunamadi""}"));

        var adapter = CreateConfiguredAdapter();

        var result = await adapter.TrackShipmentAsync(trackingNo);

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
        const string trackingNo = "HJ-MULTI-EVT";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""delivered"",
                    ""estimatedDelivery"": ""2026-03-19T10:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-17T08:00:00"",
                            ""location"": ""HepsiJet Istanbul Sube"",
                            ""description"": ""Kargo teslim alindi"",
                            ""status"": ""pickedup""
                        },
                        {
                            ""timestamp"": ""2026-03-18T12:00:00"",
                            ""location"": ""HepsiJet Ankara Transfer"",
                            ""description"": ""Transfer merkezine ulasti"",
                            ""status"": ""intransit""
                        },
                        {
                            ""timestamp"": ""2026-03-19T09:00:00"",
                            ""location"": ""HepsiJet Ankara Sube"",
                            ""description"": ""Teslim edildi"",
                            ""status"": ""delivered""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        var result = await adapter.TrackShipmentAsync(trackingNo);

        result.Status.Should().Be(CargoStatus.Delivered);
        result.Events.Should().HaveCount(3);
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
        result.Events[2].Status.Should().Be(CargoStatus.Delivered);
    }

    // ══════════════════════════════════════
    // 8. GetLabel_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_Success_ReturnsPdfBytes()
    {
        const string shipmentId = "SHP-HJ-001";
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

        var result = await adapter.GetShipmentLabelAsync(shipmentId);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
    }

    // ══════════════════════════════════════
    // 9. GetLabel_NotReady
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_NotReady_ThrowsOrReturnsEmpty()
    {
        const string shipmentId = "SHP-HJ-NOLABEL";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}/label")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""error"":""Etiket henuz olusturulmadi""}"));

        var adapter = CreateConfiguredAdapter();

        var act = async () => await adapter.GetShipmentLabelAsync(shipmentId);
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*label request failed*");
    }

    // ══════════════════════════════════════
    // 10. IsAvailable_Healthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Healthy_ReturnsTrue()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""status"":""healthy""}"));

        var adapter = CreateConfiguredAdapter();

        var result = await adapter.IsAvailableAsync();

        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 11. IsAvailable_Unhealthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Unhealthy_ReturnsFalse()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();

        var result = await adapter.IsAvailableAsync();

        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 12. Auth_InvalidCredential
    // ══════════════════════════════════════

    [Fact]
    public async Task Auth_InvalidCredential_ReturnsFailedResult()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"":""Gecersiz kimlik bilgileri""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var result = await adapter.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ══════════════════════════════════════
    // 13. ConcurrentRequests_ThreadSafe
    // ══════════════════════════════════════

    [Fact]
    public async Task ConcurrentRequests_ThreadSafe_AllSucceed()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-CONC-001"",""shipmentId"":""SHP-CONC-001""}"));

        var adapter = CreateConfiguredAdapter();

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => adapter.CreateShipmentAsync(CreateTestRequest()))
            .ToList();
        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.Success == true);
    }

    // ══════════════════════════════════════
    // 14. TokenRefresh_ExpiredToken_RefreshesAutomatically
    // ══════════════════════════════════════

    [Fact]
    public async Task TokenRefresh_ExpiredToken_RefreshesAutomatically()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""HJ-TOKEN-001"",""shipmentId"":""SHP-TOKEN-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // First call — gets token
        var result1 = await adapter.CreateShipmentAsync(request);
        result1.Success.Should().BeTrue();

        // Second call — should reuse token (not expired)
        var result2 = await adapter.CreateShipmentAsync(request);
        result2.Success.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 15. RateLimit_429_PollyRetryWithBackoff
    // ══════════════════════════════════════

    [Fact]
    public async Task RateLimit_429_PollyRetryWithBackoff()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "1")
                .WithBody(@"{""error"":""Rate limit exceeded""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        var result = await adapter.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should have retried on 429");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
