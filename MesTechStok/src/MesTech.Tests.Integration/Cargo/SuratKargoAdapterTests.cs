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
/// SuratKargoAdapter WireMock entegrasyon testleri.
/// Gercek adapter implemenasyonu REST v2 endpoint'lerine karsi test edilir.
/// Key difference: SupportsCashOnDelivery = false
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "Surat")]
public class SuratKargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<SuratKargoAdapter> _logger;

    public SuratKargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<SuratKargoAdapter>();
    }

    private SuratKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new SuratKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "surat-user",
            ["Password"] = "surat-pass",
            ["CustomerCode"] = "SURAT-CUST-001"
            // NO BaseUrl key — keeps WireMock BaseAddress
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "Test Alici",
        RecipientPhone = "05559876543",
        RecipientAddress = new Address
        {
            Street = "Test Bulvari 42",
            City = "Ankara",
            District = "Cankaya",
            PostalCode = "06100"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Cad 7",
            City = "Ankara",
            District = "Kecioren",
            PostalCode = "06280"
        },
        Weight = 1.2m,
        Desi = 3,
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
                .WithPath("/api/v2/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""ok""}"));

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
                .WithPath("/api/v2/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

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
                .WithPath("/api/v2/cargo/create")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""trackingNumber"":""SRT-456"",""shipmentId"":""SSRT-789""}"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("SRT-456");
        result.ShipmentId.Should().Be("SSRT-789");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CreateShipment_ApiError_ReturnsFailedResult()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v2/cargo/create")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Unprocessable entity - weight exceeds limit""}"));

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
                .WithPath("/api/v2/cargo/create")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert: Polly retries 3 times → total 4 attempts = 4 requests to WireMock
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
        const string trackingNo = "SRT-456";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v2/cargo/{trackingNo}/status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""out_for_delivery"",
                    ""estimatedDelivery"": ""2026-03-12T16:00:00"",
                    ""events"": [
                        {
                            ""timestamp"": ""2026-03-11T08:00:00"",
                            ""location"": ""Ankara Dagitim Merkezi"",
                            ""description"": ""Kargo dagitima cikti"",
                            ""status"": ""out_for_delivery""
                        },
                        {
                            ""timestamp"": ""2026-03-10T18:00:00"",
                            ""location"": ""Ankara Hub"",
                            ""description"": ""Kargo hub'a ulasti"",
                            ""status"": ""in_transit""
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.OutForDelivery);
        result.EstimatedDelivery.Should().NotBeNull();
        result.Events.Should().HaveCount(2);
        result.Events[0].Status.Should().Be(CargoStatus.OutForDelivery);
        result.Events[1].Status.Should().Be(CargoStatus.InTransit);
    }

    // ══════════════════════════════════════
    // 4. CancelShipment Test
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_ValidId_ReturnsTrue()
    {
        // Arrange
        const string shipmentId = "SSRT-789";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v2/cargo/{shipmentId}")
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
    // 5. SupportsCashOnDelivery Property Test
    // ══════════════════════════════════════

    [Fact]
    public void SupportsCashOnDelivery_ShouldBeFalse()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act + Assert — Surat Kargo does NOT support COD
        adapter.SupportsCashOnDelivery.Should().BeFalse(
            "Surat Kargo does not support cash on delivery (kapida odeme)");
    }

    // ══════════════════════════════════════
    // 6. GetShipmentLabel Test
    // ══════════════════════════════════════

    [Fact]
    public async Task GetShipmentLabel_ValidId_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "SSRT-789";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        var base64 = Convert.ToBase64String(fakePdfBytes);

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v2/cargo/{shipmentId}/label")
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
        result.FileName.Should().Be($"surat-label-{shipmentId}.pdf");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
