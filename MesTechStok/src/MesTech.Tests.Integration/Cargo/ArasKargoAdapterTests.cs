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
/// ArasKargoAdapter WireMock entegrasyon testleri.
/// Gercek adapter implemenasyonu REST endpoint'lerine karsi test edilir.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "Aras")]
public class ArasKargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<ArasKargoAdapter> _logger;

    public ArasKargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<ArasKargoAdapter>();
    }

    private ArasKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new ArasKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test-user",
            ["Password"] = "test-pass",
            ["CustomerCode"] = "CUST-001"
            // NO BaseUrl key — keeps WireMock BaseAddress
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "Test Alici",
        RecipientPhone = "05551234567",
        RecipientAddress = new Address
        {
            Street = "Test Sok 1",
            City = "Istanbul",
            District = "Kadikoy",
            PostalCode = "34710"
        },
        SenderAddress = new Address
        {
            Street = "Sender Sok 1",
            City = "Istanbul",
            District = "Besiktas",
            PostalCode = "34353"
        },
        Weight = 2.5m,
        Desi = 5,
        ParcelCount = 1
    };

    // ══════════════════════════════════════
    // 1. IsAvailable Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_WhenApiHealthy_ReturnsTrue()
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
    }

    [Fact]
    public async Task IsAvailable_WhenApiDown_ReturnsFalse()
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
    }

    // ══════════════════════════════════════
    // 2. CreateShipment Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ValidRequest_ReturnsTrackingNumber()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""ARS-123"",""shipmentId"":""SHP-001""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("ARS-123");
        result.ShipmentId.Should().Be("SHP-001");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CreateShipment_ApiError_ReturnsFailedResult()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Bad request - invalid address""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    [Fact]
    public async Task CreateShipment_ServerError_PollyRetries()
    {
        // Arrange — always return 500 to trigger all Polly retries
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

        // Assert: Polly retries 3 times → total 4 attempts = 4 log entries minimum
        result.Success.Should().BeFalse();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should have retried at least once, generating multiple requests");
    }

    // ══════════════════════════════════════
    // 3. TrackShipment Test
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_ValidTracking_ReturnsEvents()
    {
        // Arrange
        const string trackingNo = "ARS-123";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""intransit"",
                    ""estimatedDelivery"": ""2026-03-15T14:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-10T09:00:00"",
                            ""location"": ""Istanbul Depo"",
                            ""description"": ""Kargo teslim alindi"",
                            ""status"": ""pickedup""
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
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
        result.Events[0].Location.Should().Be("Istanbul Depo");
    }

    // ══════════════════════════════════════
    // 4. CancelShipment Test
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_ValidId_ReturnsTrue()
    {
        // Arrange
        const string shipmentId = "SHP-001";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{shipmentId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync(shipmentId);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 5. GetShipmentLabel Test
    // ══════════════════════════════════════

    [Fact]
    public async Task GetShipmentLabel_ValidId_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "SHP-001";
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
        result.FileName.Should().Be($"aras-label-{shipmentId}.pdf");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
