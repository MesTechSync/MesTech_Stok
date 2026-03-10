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
/// SuratKargoAdapter contract tests — validates REST adapter behavior against
/// WireMock stubs using CargoWireMockHelper.
/// REST adapter: Basic Auth, JSON payloads.
/// Endpoints: /api/v2/cargo/create, /api/v2/health, etc.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cargo", "SuratKargo")]
public class SuratKargoContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<SuratKargoAdapter> _logger;

    public SuratKargoContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<SuratKargoAdapter>();
    }

    private SuratKargoAdapter CreateUnconfiguredAdapter()
    {
        var httpClient = new HttpClient();
        return new SuratKargoAdapter(httpClient, _logger);
    }

    private SuratKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new SuratKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "surat-test-user",
            ["Password"] = "surat-test-pass",
            ["CustomerCode"] = "CUST-67890",
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
    public void Provider_ReturnsSuratKargo()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act & Assert
        adapter.Provider.Should().Be(CargoProvider.SuratKargo);
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
        adapter.SupportsCancellation.Should().BeTrue("Surat Kargo supports cancellation");
        adapter.SupportsLabelGeneration.Should().BeTrue("Surat Kargo supports label generation");
        adapter.SupportsCashOnDelivery.Should().BeFalse("Surat Kargo does not support cash on delivery");
        adapter.SupportsMultiParcel.Should().BeFalse("Surat Kargo does not support multi-parcel");
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
        const string expectedTracking = "SURAT987654321";
        const string expectedShipmentId = "SHP-SURAT-001";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v2/cargo/create")
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
                .WithPath("/api/v2/cargo/create")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(CargoWireMockHelper.BuildErrorResponse(400, "Gecersiz musteri kodu")));

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
        const string trackingNo = "SURAT987654321";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/v2/cargo/{trackingNo}/status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(CargoWireMockHelper.BuildTrackingResponse(
                    trackingNo,
                    "delivered",
                    new[]
                    {
                        ("2026-03-10T10:00:00Z", "Istanbul Depo", "Kargo teslim alindi", "picked_up"),
                        ("2026-03-11T18:00:00Z", "Alici Adresi", "Teslim edildi", "delivered")
                    })));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.Delivered);
        result.Events.Should().HaveCount(2);
        result.Events[0].Location.Should().Be("Istanbul Depo");
        result.Events[1].Location.Should().Be("Alici Adresi");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
