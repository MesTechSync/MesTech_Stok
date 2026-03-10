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

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// ArasKargoAdapter contract tests — validates REST adapter behavior against
/// WireMock stubs using CargoWireMockHelper.
/// REST adapter: Basic Auth, JSON payloads.
/// Endpoints: /api/v1/shipments, /api/v1/health, etc.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cargo", "ArasKargo")]
public class ArasKargoContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<ArasKargoAdapter> _logger;

    public ArasKargoContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<ArasKargoAdapter>();
    }

    private ArasKargoAdapter CreateUnconfiguredAdapter()
    {
        var httpClient = new HttpClient();
        return new ArasKargoAdapter(httpClient, _logger);
    }

    private ArasKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new ArasKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "aras-test-user",
            ["Password"] = "aras-test-pass",
            ["CustomerCode"] = "CUST-12345",
            ["BaseUrl"] = _fixture.BaseUrl
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestShipmentRequest() => new()
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "Test Alici",
        RecipientPhone = "05551234567",
        RecipientAddress = new Address
        {
            Street = "Test Mah. Test Sok. No:1",
            City = "Istanbul",
            District = "Kadikoy",
            PostalCode = "34710"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Mah. Depo Sok. No:5",
            City = "Istanbul",
            District = "Umraniye",
            PostalCode = "34764"
        },
        Desi = 3,
        Weight = 2.5m,
        ParcelCount = 1
    };

    // ══════════════════════════════════════
    // 1. Provider Identity
    // ══════════════════════════════════════

    [Fact]
    public void Provider_ReturnsArasKargo()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act & Assert
        adapter.Provider.Should().Be(CargoProvider.ArasKargo);
    }

    // ══════════════════════════════════════
    // 2. Capabilities
    // ══════════════════════════════════════

    [Fact]
    public void Capabilities_CorrectFlags()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act & Assert
        adapter.SupportsCancellation.Should().BeTrue("Aras Kargo supports cancellation");
        adapter.SupportsLabelGeneration.Should().BeTrue("Aras Kargo supports label generation");
        adapter.SupportsCashOnDelivery.Should().BeTrue("Aras Kargo supports cash on delivery");
        adapter.SupportsMultiParcel.Should().BeFalse("Aras Kargo does not support multi-parcel");
    }

    // ══════════════════════════════════════
    // 3. IsAvailableAsync — Unconfigured
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailableAsync_Unconfigured_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateUnconfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse("unconfigured adapter should not be available");
        _mockServer.LogEntries.Should().BeEmpty("no HTTP call should be made when unconfigured");
    }

    // ══════════════════════════════════════
    // 4. CreateShipmentAsync — Unconfigured
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipmentAsync_Unconfigured_Throws()
    {
        // Arrange
        var adapter = CreateUnconfiguredAdapter();
        var request = CreateTestShipmentRequest();

        // Act & Assert
        var act = () => adapter.CreateShipmentAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*konfigure*");
    }

    // ══════════════════════════════════════
    // 5. CreateShipmentAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipmentAsync_Success_ReturnsTrackingNumber()
    {
        // Arrange
        const string expectedTracking = "ARAS123456789";
        const string expectedShipmentId = "SHP-ARAS-001";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(CargoWireMockHelper.BuildCreateShipmentResponse(
                    expectedTracking, expectedShipmentId)));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestShipmentRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be(expectedTracking);
        result.ShipmentId.Should().Be(expectedShipmentId);
        result.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 6. CreateShipmentAsync — API Error
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipmentAsync_ApiError400_ReturnsFailedResult()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/shipments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(CargoWireMockHelper.BuildErrorResponse(400, "Gecersiz adres bilgisi")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestShipmentRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 7. TrackShipmentAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipmentAsync_Success_ReturnsStatusAndEvents()
    {
        // Arrange
        const string trackingNo = "ARAS123456789";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v1/shipments/{trackingNo}/tracking")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(CargoWireMockHelper.BuildTrackingResponse(
                    trackingNo,
                    "in_transit",
                    new[]
                    {
                        ("2026-03-10T10:00:00Z", "Istanbul Depo", "Kargo teslim alindi", "picked_up"),
                        ("2026-03-11T14:00:00Z", "Ankara Hub", "Transfer edildi", "in_transit")
                    })));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.InTransit);
        result.Events.Should().HaveCount(2);
        result.Events[0].Location.Should().Be("Istanbul Depo");
        result.Events[1].Location.Should().Be("Ankara Hub");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
