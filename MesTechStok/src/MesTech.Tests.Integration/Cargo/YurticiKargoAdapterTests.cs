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
/// YurticiKargoAdapter WireMock entegrasyon testleri.
/// Gercek adapter SOAP isteklerini SimpleSoapClient uzerinden gonderir.
/// WireMock SOAPAction header ile ayirt eder.
/// Key: SupportsCancellation = false
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "Yurtici")]
public class YurticiKargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<YurticiKargoAdapter> _logger;

    private const string SoapPath = "/yurticikargo/soap";

    public YurticiKargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<YurticiKargoAdapter>();
    }

    private YurticiKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient();
        var adapter = new YurticiKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "yk-user",
            ["Password"] = "yk-pass",
            ["ServiceUrl"] = _fixture.BaseUrl + SoapPath  // WireMock absolute URL
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        RecipientName = "Mehmet Yilmaz",
        RecipientPhone = "05321234567",
        RecipientAddress = new Address
        {
            Street = "Ataturk Cad 100",
            City = "Izmir",
            District = "Konak",
            PostalCode = "35010"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Sok 5",
            City = "Izmir",
            District = "Bornova",
            PostalCode = "35100"
        },
        Weight = 3.0m,
        Desi = 8,
        ParcelCount = 2
    };

    // ── SOAP response helpers ─────────────────────────────────

    private static string SoapOkResponse(string innerXml) =>
        $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soapenv:Body>
    {innerXml}
  </soapenv:Body>
</soapenv:Envelope>";

    private static string SoapFaultResponse(string faultString) =>
        $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soapenv:Body>
    <soapenv:Fault>
      <faultcode>soapenv:Server</faultcode>
      <faultstring>{faultString}</faultstring>
    </soapenv:Fault>
  </soapenv:Body>
</soapenv:Envelope>";

    // ══════════════════════════════════════
    // 1. IsAvailable Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_SoapResponds_ReturnsTrue()
    {
        // Arrange — adapter pings via queryShipment with "PING-TEST"
        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/queryShipment"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<queryShipmentResponse>
  <return>
    <operationCode>1</operationCode>
  </return>
</queryShipmentResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailable_SoapFails_ReturnsFalse()
    {
        // Arrange — return HTTP 500 to cause exception → IsAvailable catches and returns false
        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("SOAP endpoint unreachable"));

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
    public async Task CreateShipment_ValidSoapRequest_ReturnsTrackingNumber()
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
                .WithBody(SoapOkResponse(@"
<createShipmentResponse>
  <return>
    <cargoKey>YK123456789</cargoKey>
    <invDocId>INV-001</invDocId>
    <jobId>JOB-001</jobId>
    <result>OK</result>
  </return>
</createShipmentResponse>")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("YK123456789");
        result.ShipmentId.Should().Be("JOB-001");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CreateShipment_SoapFaultResponse_ReturnsFailedResult()
    {
        // Arrange — SOAP Fault response
        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/createShipment"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapFaultResponse("Alici adres gecersiz - sehir kodu bulunamadi")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — SOAP Fault is caught and returned as failed
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 3. TrackShipment Test
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_ValidTrackingNumber_ReturnsStatusAndEvents()
    {
        // Arrange
        const string trackingNo = "YK123456789";

        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/queryShipment"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<queryShipmentResponse>
  <return>
    <operationCode>3</operationCode>
    <estimatedArrivalDate>2026-03-14</estimatedArrivalDate>
    <ShipmentEventVO>
      <eventDate>2026-03-10T10:00:00</eventDate>
      <unitName>Istanbul Depo</unitName>
      <eventName>Kargo teslim alindi</eventName>
      <operationCode>2</operationCode>
    </ShipmentEventVO>
    <ShipmentEventVO>
      <eventDate>2026-03-11T14:00:00</eventDate>
      <unitName>Izmir Hub</unitName>
      <eventName>Transfer edildi</eventName>
      <operationCode>3</operationCode>
    </ShipmentEventVO>
  </return>
</queryShipmentResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.InTransit); // operationCode "3"
        result.EstimatedDelivery.Should().NotBeNull();
        result.Events.Should().HaveCount(2);
        result.Events[0].Location.Should().Be("Istanbul Depo");
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp); // operationCode "2"
        result.Events[1].Location.Should().Be("Izmir Hub");
        result.Events[1].Status.Should().Be(CargoStatus.InTransit); // operationCode "3"
    }

    // ══════════════════════════════════════
    // 4. CancelShipment — Not Supported
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_NotSupported_ReturnsFalse()
    {
        // Arrange — no WireMock stub needed; adapter logs warning and returns false without HTTP call
        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("YK123456789");

        // Assert
        result.Should().BeFalse("Yurtici Kargo does not support API cancellation");
        _mockServer.LogEntries.Should().BeEmpty("No HTTP call should be made for unsupported cancel");
    }

    // ══════════════════════════════════════
    // 5. GetShipmentLabel Test
    // ══════════════════════════════════════

    [Fact]
    public async Task GetShipmentLabel_ValidId_ReturnsPdfBytes()
    {
        // Arrange
        const string shipmentId = "YK123456789";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        var base64 = Convert.ToBase64String(fakePdfBytes);

        _mockServer
            .Given(Request.Create()
                .WithPath(SoapPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://yurticikargo.com/createShipmentLabel"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse($@"
<createShipmentLabelResponse>
  <return>
    <labelData>{base64}</labelData>
    <labelFormat>PDF</labelFormat>
  </return>
</createShipmentLabelResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.GetShipmentLabelAsync(shipmentId);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        result.FileName.Should().Be($"yk-label-{shipmentId}.pdf");
    }

    // ══════════════════════════════════════
    // 6. SupportsCancellation Property Test
    // ══════════════════════════════════════

    [Fact]
    public void SupportsCancellation_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateConfiguredAdapter();

        // Act + Assert — Yurtici Kargo does NOT support API cancellation
        adapter.SupportsCancellation.Should().BeFalse(
            "Yurtici Kargo does not expose a cancellation API endpoint");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
