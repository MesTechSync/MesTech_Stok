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
/// YurticiKargoAdapter contract tests — validates adapter behavior against
/// SOAP WireMock stubs using CargoWireMockHelper.
/// SOAP adapter: all requests POST to ServiceUrl path, differentiated by SOAPAction.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cargo", "YurticiKargo")]
public class YurticiKargoContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<YurticiKargoAdapter> _logger;

    private const string SoapPath = "/ws/ShipmentService";

    public YurticiKargoContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<YurticiKargoAdapter>();
    }

    private YurticiKargoAdapter CreateUnconfiguredAdapter()
    {
        var httpClient = new HttpClient();
        return new YurticiKargoAdapter(httpClient, _logger);
    }

    private YurticiKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient();
        var adapter = new YurticiKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "yk-test-user",
            ["Password"] = "yk-test-pass",
            ["UserLanguage"] = "TR",
            ["ServiceUrl"] = _fixture.BaseUrl + SoapPath
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
    public void Provider_ReturnsYurticiKargo()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act & Assert
        adapter.Provider.Should().Be(CargoProvider.YurticiKargo);
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
        adapter.SupportsCancellation.Should().BeFalse("Yurtici Kargo does not support API cancellation");
        adapter.SupportsLabelGeneration.Should().BeTrue("Yurtici Kargo supports label generation");
        adapter.SupportsCashOnDelivery.Should().BeTrue("Yurtici Kargo supports cash on delivery");
        adapter.SupportsMultiParcel.Should().BeTrue("Yurtici Kargo supports multi-parcel shipments");
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
        const string expectedTracking = "YK987654321";
        const string expectedJobId = "JOB-9876";

        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/createShipment"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(CargoWireMockHelper.BuildSoapCreateShipmentResponse(
                    expectedTracking, expectedJobId)));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestShipmentRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be(expectedTracking);
        result.ShipmentId.Should().Be(expectedJobId);
        result.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 6. CreateShipmentAsync — SOAP Fault
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipmentAsync_SoapFault_ReturnsFailedResult()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/createShipment"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(CargoWireMockHelper.BuildSoapFault(
                    "soapenv:Server", "Gecersiz alici sehir kodu")));

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
    // 7. GetShipmentLabelAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetShipmentLabelAsync_Success_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "YK-LABEL-001";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var base64 = Convert.ToBase64String(fakePdfBytes);

        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/createShipmentLabel"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(CargoWireMockHelper.BuildSoapLabelResponse(base64)));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.GetShipmentLabelAsync(shipmentId);

        // Assert
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        result.FileName.Should().Contain(shipmentId);
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
