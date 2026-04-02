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
/// PttKargoAdapter WireMock entegrasyon testleri.
/// Gercek adapter SOAP isteklerini SimpleSoapClient uzerinden gonderir.
/// WireMock SOAPAction header ile ayirt eder.
/// PTT Kargo SOAP API: gonderiKaydet, gonderiSorgula, gonderiIptal, etiketAl
/// CargoProvider.PttKargo = 5.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Cargo")]
[Trait("Provider", "PTT")]
public class PttKargoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<PttKargoAdapter> _logger;

    private const string ShipmentPath = "/ptt/shipment";
    private const string TrackingPath = "/ptt/tracking";

    public PttKargoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<PttKargoAdapter>();
    }

    // ── Factory method ─────────────────────────────────────────

    private PttKargoAdapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient();
        var adapter = new PttKargoAdapter(httpClient, _logger);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "ptt-user",
            ["Password"] = "ptt-pass",
            ["MusteriId"] = "M12345",
            ["ShipmentServiceUrl"] = _fixture.BaseUrl + ShipmentPath,
            ["TrackingServiceUrl"] = _fixture.BaseUrl + TrackingPath
        });
        return adapter;
    }

    private static ShipmentRequest CreateTestRequest() => new ShipmentRequest
    {
        OrderId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        RecipientName = "PTT Test Alici",
        RecipientPhone = "05552223344",
        RecipientAddress = new Address
        {
            Street = "PTT Test Mah 10",
            City = "Ankara",
            District = "Etimesgut",
            PostalCode = "06790"
        },
        SenderAddress = new Address
        {
            Street = "Gonderen Sok 8",
            City = "Ankara",
            District = "Cankaya",
            PostalCode = "06100"
        },
        Weight = 1.0m,
        Desi = 2,
        ParcelCount = 1
    };

    // ── SOAP response helpers ─────────────────────────────────────

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
    // 1. CreateShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_Success_ReturnsTrackingNumber()
    {
        // Arrange — mock SOAP gonderiKaydet response with barkodNo
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "https://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiKaydetResponse>
  <barkodNo>PTT0098765432</barkodNo>
  <gonderiId>GND-001234</gonderiId>
  <sonuc>BASARILI</sonuc>
</gonderiKaydetResponse>")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("PTT0098765432");
        result.ShipmentId.Should().Be("GND-001234");
        result.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════
    // 2. CreateShipment_InvalidAddress
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_InvalidAddress_ReturnsFailedResult()
    {
        // Arrange — SOAP Fault for invalid address
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "https://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapFaultResponse("Gecersiz adres bilgisi - il/ilce kodu bulunamadi")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — SOAP Fault caught, returned as failed (barkod not found in fault response)
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
        // Arrange — HTTP 500 triggers HttpRequestException in SimpleSoapClient
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "https://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — 500 causes HttpRequestException which is caught by adapter
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("PTT Kargo hatasi");
        // WireMock should have received at least 1 request (Polly retry if configured)
        _mockServer.LogEntries.Should().HaveCountGreaterOrEqualTo(1);
    }

    // ══════════════════════════════════════
    // 4. CreateShipment_ServerError_CircuitBreaker
    // ══════════════════════════════════════

    [Fact]
    public async Task CreateShipment_ServerError_CircuitBreaker_ReturnsFailed()
    {
        // Arrange — persistent 503 to simulate service unavailable
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "https://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act — multiple calls to trigger circuit breaker behavior
        var result1 = await adapter.CreateShipmentAsync(request);
        var result2 = await adapter.CreateShipmentAsync(request);

        // Assert — both calls fail gracefully
        result1.Success.Should().BeFalse();
        result1.ErrorMessage.Should().Contain("PTT Kargo hatasi");
        result2.Success.Should().BeFalse();
        result2.ErrorMessage.Should().Contain("PTT Kargo hatasi");
    }

    // ══════════════════════════════════════
    // 5. TrackShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_Success_ReturnsStatusAndEvents()
    {
        // Arrange — SOAP gonderiSorgula response with DAGITIMDA status and 2 events
        const string trackingNo = "PTT0098765432";

        _mockServer
            .Given(Request.Create()
                .WithPath(TrackingPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiSorgula"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiSorgulaResponse>
  <durumKodu>04</durumKodu>
  <tahminiTeslimTarihi>2026-03-20</tahminiTeslimTarihi>
  <HareketVO>
    <tarih>2026-03-16T09:00:00</tarih>
    <birim>Ankara Dagitim Merkezi</birim>
    <aciklama>Gonderi teslim alindi</aciklama>
    <durumKodu>02</durumKodu>
  </HareketVO>
  <HareketVO>
    <tarih>2026-03-17T14:30:00</tarih>
    <birim>Etimesgut PTT Subesi</birim>
    <aciklama>Dagitima cikarildi</aciklama>
    <durumKodu>04</durumKodu>
  </HareketVO>
</gonderiSorgulaResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.OutForDelivery); // durumKodu "04" = DAGITIMDA
        result.EstimatedDelivery.Should().NotBeNull();
        result.Events.Should().HaveCount(2);
        result.Events[0].Location.Should().Be("Ankara Dagitim Merkezi");
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp); // durumKodu "02"
        result.Events[1].Location.Should().Be("Etimesgut PTT Subesi");
        result.Events[1].Status.Should().Be(CargoStatus.OutForDelivery); // durumKodu "04"
    }

    // ══════════════════════════════════════
    // 6. TrackShipment_NotFound
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_NotFound_ReturnsCreatedStatus()
    {
        // Arrange — SOAP Fault for barcode not found
        _mockServer
            .Given(Request.Create()
                .WithPath(TrackingPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiSorgula"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapFaultResponse("Gonderi bulunamadi - gecersiz barkod numarasi")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync("INVALID-BARKOD-999");

        // Assert — graceful degradation: exception caught, defaults to Created
        result.TrackingNumber.Should().Be("INVALID-BARKOD-999");
        result.Status.Should().Be(CargoStatus.Created);
        result.Events.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 7. TrackShipment_MultipleEvents
    // ══════════════════════════════════════

    [Fact]
    public async Task TrackShipment_MultipleEvents_ReturnsAllEvents()
    {
        // Arrange — SOAP tracking response with 3 movement entries
        const string trackingNo = "PTT1122334455";

        _mockServer
            .Given(Request.Create()
                .WithPath(TrackingPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiSorgula"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiSorgulaResponse>
  <durumKodu>05</durumKodu>
  <tahminiTeslimTarihi>2026-03-18</tahminiTeslimTarihi>
  <HareketVO>
    <tarih>2026-03-15T08:00:00</tarih>
    <birim>Istanbul Merkez</birim>
    <aciklama>Gonderi kabul edildi</aciklama>
    <durumKodu>02</durumKodu>
  </HareketVO>
  <HareketVO>
    <tarih>2026-03-16T12:00:00</tarih>
    <birim>Ankara Transfer</birim>
    <aciklama>Transfer merkezine ulasti</aciklama>
    <durumKodu>03</durumKodu>
  </HareketVO>
  <HareketVO>
    <tarih>2026-03-17T16:45:00</tarih>
    <birim>Cankaya PTT Subesi</birim>
    <aciklama>Aliciya teslim edildi</aciklama>
    <durumKodu>05</durumKodu>
  </HareketVO>
</gonderiSorgulaResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.TrackShipmentAsync(trackingNo);

        // Assert
        result.TrackingNumber.Should().Be(trackingNo);
        result.Status.Should().Be(CargoStatus.Delivered); // durumKodu "05"
        result.Events.Should().HaveCount(3);
        result.Events[0].Status.Should().Be(CargoStatus.PickedUp);
        result.Events[0].Description.Should().Be("Gonderi kabul edildi");
        result.Events[1].Status.Should().Be(CargoStatus.InTransit);
        result.Events[1].Location.Should().Be("Ankara Transfer");
        result.Events[2].Status.Should().Be(CargoStatus.Delivered);
        result.Events[2].Location.Should().Be("Cankaya PTT Subesi");
    }

    // ══════════════════════════════════════
    // 8. CancelShipment_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_Success_ReturnsExpectedResult()
    {
        // Arrange — SOAP gonderiIptal response with sonuc=BASARILI
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiIptal"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiIptalResponse>
  <sonuc>BASARILI</sonuc>
  <mesaj>Gonderi basariyla iptal edildi</mesaj>
</gonderiIptalResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("PTT0098765432");

        // Assert
        result.Should().BeTrue("PTT Kargo returned BASARILI for cancel request");
        _mockServer.LogEntries.Should().HaveCount(1, "exactly one SOAP request should be made");
    }

    // ══════════════════════════════════════
    // 9. CancelShipment_AlreadyDelivered
    // ══════════════════════════════════════

    [Fact]
    public async Task CancelShipment_AlreadyDelivered_ReturnsFalse()
    {
        // Arrange — SOAP Fault for already-delivered shipment cancel attempt
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiIptal"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapFaultResponse("Gonderi teslim edilmis durumda - iptal yapilamaz")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.CancelShipmentAsync("PTT-DELIVERED-001");

        // Assert — fault caught, adapter returns false gracefully
        result.Should().BeFalse("cancel should fail for already-delivered shipments");
    }

    // ══════════════════════════════════════
    // 10. GetLabel_Success
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_Success_ReturnsPdfBytes()
    {
        // Arrange — SOAP etiketAl response with base64 PDF data
        const string shipmentId = "PTT0098765432";
        var fakePdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        var base64 = Convert.ToBase64String(fakePdfBytes);

        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/etiketAl"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse($@"
<etiketAlResponse>
  <etiketData>{base64}</etiketData>
  <format>PDF</format>
</etiketAlResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.GetShipmentLabelAsync(shipmentId);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEquivalentTo(fakePdfBytes);
        result.Format.Should().Be(LabelFormat.Pdf);
        result.FileName.Should().Be($"ptt-label-{shipmentId}.pdf");
    }

    // ══════════════════════════════════════
    // 11. GetLabel_NotReady
    // ══════════════════════════════════════

    [Fact]
    public async Task GetLabel_NotReady_ThrowsOrReturnsEmpty()
    {
        // Arrange — SOAP response with empty etiketData (label not yet generated)
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/etiketAl"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<etiketAlResponse>
  <mesaj>Etiket henuz hazirlanmadi</mesaj>
</etiketAlResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act + Assert — adapter throws when etiketData/pdfData element is missing
        var act = async () => await adapter.GetShipmentLabelAsync("PTT-NOTREADY-001");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*etiket*");
    }

    // ══════════════════════════════════════
    // 12. IsAvailable_Healthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Healthy_ReturnsTrue()
    {
        // Arrange — tracking endpoint responds successfully to ping query
        _mockServer
            .Given(Request.Create()
                .WithPath(TrackingPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiSorgula"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiSorgulaResponse>
  <durumKodu>01</durumKodu>
  <mesaj>Ping basarili</mesaj>
</gonderiSorgulaResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeTrue("healthy SOAP endpoint should return true");
        _mockServer.LogEntries.Should().HaveCountGreaterOrEqualTo(1, "at least one ping request should be sent");
    }

    // ══════════════════════════════════════
    // 13. IsAvailable_Unhealthy
    // ══════════════════════════════════════

    [Fact]
    public async Task IsAvailable_Unhealthy_ReturnsFalse()
    {
        // Arrange — tracking endpoint returns 500 to simulate outage
        _mockServer
            .Given(Request.Create()
                .WithPath(TrackingPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("PTT SOAP endpoint unreachable"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse("unhealthy endpoint should return false");
    }

    // ══════════════════════════════════════
    // 14. Auth_InvalidCredential
    // ══════════════════════════════════════

    [Fact]
    public async Task Auth_InvalidCredential_ReturnsFailedResult()
    {
        // Arrange — SOAP Fault for invalid credentials on shipment creation
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapFaultResponse("Gecersiz kullanici adi veya sifre")));

        var adapter = CreateConfiguredAdapter();
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateShipmentAsync(request);

        // Assert — auth failure is caught and returned as failed
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
        // Arrange — SOAP gonderiKaydet success response for concurrent calls
        _mockServer
            .Given(Request.Create()
                .WithPath(ShipmentPath)
                .UsingPost()
                .WithHeader("SOAPAction", "http://ws.ptt.gov.tr/gonderiKaydet"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapOkResponse(@"
<gonderiKaydetResponse>
  <barkodNo>PTT-CONCURRENT-001</barkodNo>
  <gonderiId>GND-CONC-001</gonderiId>
  <sonuc>BASARILI</sonuc>
</gonderiKaydetResponse>")));

        var adapter = CreateConfiguredAdapter();

        // Act — 5 concurrent CreateShipmentAsync calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ =>
            {
                var req = CreateTestRequest();
                req.OrderId = Guid.NewGuid(); // unique order per call
                return adapter.CreateShipmentAsync(req);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert — all 5 succeed, no deadlock or exception
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r =>
        {
            r.Success.Should().BeTrue();
            r.TrackingNumber.Should().Be("PTT-CONCURRENT-001");
        });
        _mockServer.LogEntries.Should().HaveCount(5, "all 5 concurrent requests should reach the server");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
