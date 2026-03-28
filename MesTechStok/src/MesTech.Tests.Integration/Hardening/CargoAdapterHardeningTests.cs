using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.ValueObjects;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Hardening;

/// <summary>
/// Cargo Adapter Hardening Tests — DEV-H5
/// 7 kargo adapters x 4 scenarios = 28 tests
///
/// Scenarios:
///   1. Happy path (CreateShipment) — success + tracking number
///   2. Invalid address — graceful error, no crash
///   3. API down (timeout/500) — retry then graceful fail
///   4. Label format — returns correct format (PDF/ZPL)
/// </summary>
[Trait("Category", "Hardening")]
[Trait("Sprint", "DEV-H5")]
public class CargoAdapterHardeningTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly string _baseUrl;

    public CargoAdapterHardeningTests()
    {
        _server = WireMockServer.Start();
        _baseUrl = _server.Url!;
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ShipmentRequest CreateValidRequest() => new()
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "Ali Yilmaz",
        RecipientPhone = "+905551234567",
        RecipientAddress = new Address
        {
            Street = "Ataturk Cad. No:42",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            Country = "TR"
        },
        SenderAddress = new Address
        {
            Street = "Sanayi Mah. Depo Sk. No:1",
            District = "Gebze",
            City = "Kocaeli",
            PostalCode = "41400",
            Country = "TR"
        },
        Weight = 2.5m,
        Desi = 3,
        ParcelCount = 1,
        Notes = "Kirilacak urun"
    };

    private static ShipmentRequest CreateInvalidAddressRequest() => new()
    {
        OrderId = Guid.NewGuid(),
        RecipientName = "",
        RecipientPhone = "",
        RecipientAddress = new Address
        {
            Street = "",
            District = "",
            City = "",
            PostalCode = "",
            Country = ""
        },
        SenderAddress = new Address
        {
            Street = "",
            District = "",
            City = "",
            PostalCode = "",
            Country = ""
        },
        Weight = -1m,
        Desi = 0,
        ParcelCount = 0
    };

    private static string JsonOk(string trackingNumber = "TRK123456789", string shipmentId = "SH-001")
    {
        return JsonSerializer.Serialize(new
        {
            trackingNumber,
            shipmentId,
            status = "created"
        });
    }

    private static string JsonLabelOk(string shipmentId = "SH-001")
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF magic
        return JsonSerializer.Serialize(new
        {
            labelData = Convert.ToBase64String(pdfBytes),
            format = "pdf"
        });
    }

    private static string JsonError(string message = "Bad Request")
    {
        return JsonSerializer.Serialize(new { error = message });
    }

    // SOAP envelope helpers for Yurtici and PTT
    private static string SoapOk(string trackingElement, string trackingValue)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <createShipmentResponse>
      <{trackingElement}>{trackingValue}</{trackingElement}>
      <jobId>JOB-001</jobId>
    </createShipmentResponse>
  </soap:Body>
</soap:Envelope>";
    }

    private static string SoapLabelOk()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <createShipmentLabelResponse>
      <labelData>{Convert.ToBase64String(pdfBytes)}</labelData>
    </createShipmentLabelResponse>
  </soap:Body>
</soap:Envelope>";
    }

    private static string SoapFault(string message = "Server Error")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <soap:Fault>
      <faultcode>soap:Server</faultcode>
      <faultstring>{message}</faultstring>
    </soap:Fault>
  </soap:Body>
</soap:Envelope>";
    }

    // PTT SOAP responses
    private static string PttSoapOk(string barkod = "PTT123456789")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <gonderiKaydetResponse>
      <barkodNo>{barkod}</barkodNo>
      <gonderiId>GND-001</gonderiId>
    </gonderiKaydetResponse>
  </soap:Body>
</soap:Envelope>";
    }

    private static string PttSoapLabelOk()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <etiketAlResponse>
      <etiketData>{Convert.ToBase64String(pdfBytes)}</etiketData>
    </etiketAlResponse>
  </soap:Body>
</soap:Envelope>";
    }

    // Configure adapters
    private ArasKargoAdapter CreateArasAdapter(HttpClient httpClient)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var adapter = new ArasKargoAdapter(httpClient, NullLogger<ArasKargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test",
            ["Password"] = "test",
            ["CustomerCode"] = "C001",
            ["BaseUrl"] = _baseUrl
        });
        return adapter;
    }

    private SuratKargoAdapter CreateSuratAdapter(HttpClient httpClient)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var adapter = new SuratKargoAdapter(httpClient, NullLogger<SuratKargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test",
            ["Password"] = "test",
            ["CustomerCode"] = "C002",
            ["BaseUrl"] = _baseUrl
        });
        return adapter;
    }

    private MngKargoAdapter CreateMngAdapter(HttpClient httpClient)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var adapter = new MngKargoAdapter(httpClient, NullLogger<MngKargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["ApiKey"] = "test-key",
            ["ApiSecret"] = "test-secret",
            ["CustomerCode"] = "C003",
            ["BaseUrl"] = _baseUrl
        });
        return adapter;
    }

    private HepsiJetCargoAdapter CreateHepsiJetAdapter(HttpClient httpClient)
    {
        // HepsiJet needs a token endpoint for EnsureTokenAsync
        _server.Given(
            Request.Create().WithPath("/api/v1/auth/token").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    accessToken = "test-token-123",
                    expiresIn = 3600
                }))
        );

        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var adapter = new HepsiJetCargoAdapter(httpClient, NullLogger<HepsiJetCargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test",
            ["Password"] = "test",
            ["CustomerCode"] = "C004",
            ["BaseUrl"] = _baseUrl
        });
        return adapter;
    }

    private SendeoCargoAdapter CreateSendeoAdapter(HttpClient httpClient)
    {
        var adapter = new SendeoCargoAdapter(httpClient, NullLogger<SendeoCargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["ApiKey"] = "sendeo-test-key",
            ["CustomerCode"] = "C005",
            ["BaseUrl"] = _baseUrl
        });
        return adapter;
    }

    private YurticiKargoAdapter CreateYurticiAdapter(HttpClient httpClient)
    {
        var options = Options.Create(new YurticiKargoOptions
        {
            ProductionServiceUrl = _baseUrl + "/soap/yurtici",
            SandboxServiceUrl = _baseUrl + "/soap/yurtici",
            UseSandbox = false
        });
        var adapter = new YurticiKargoAdapter(httpClient, NullLogger<YurticiKargoAdapter>.Instance, options);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test",
            ["Password"] = "test",
            ["ServiceUrl"] = _baseUrl + "/soap/yurtici"
        });
        return adapter;
    }

    private PttKargoAdapter CreatePttAdapter(HttpClient httpClient)
    {
        var adapter = new PttKargoAdapter(httpClient, NullLogger<PttKargoAdapter>.Instance);
        adapter.Configure(new Dictionary<string, string>
        {
            ["UserName"] = "test",
            ["Password"] = "test",
            ["MusteriId"] = "M001",
            ["ShipmentServiceUrl"] = _baseUrl + "/soap/ptt-shipment",
            ["TrackingServiceUrl"] = _baseUrl + "/soap/ptt-tracking"
        });
        return adapter;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 1. YURTICI KARGO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task YurticiKargo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/yurtici").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapOk("cargoKey", "YK123456789"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateYurticiAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().NotBeNullOrEmpty();
        result.TrackingNumber.Should().Be("YK123456789");
    }

    [Fact]
    public async Task YurticiKargo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange — server returns SOAP fault for invalid data
        _server.Given(
            Request.Create().WithPath("/soap/yurtici").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapFault("Alici adresi gecersiz"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateYurticiAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task YurticiKargo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange — server returns 500
        _server.Given(
            Request.Create().WithPath("/soap/yurtici").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error")
        );

        var httpClient = new HttpClient();
        var adapter = CreateYurticiAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task YurticiKargo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/yurtici").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateYurticiAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("YK123456789");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("yk-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. ARAS KARGO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ArasKargo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonOk("ARAS987654321", "ARAS-SH-001"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateArasAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("ARAS987654321");
        result.ShipmentId.Should().Be("ARAS-SH-001");
    }

    [Fact]
    public async Task ArasKargo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonError("Alici adresi gecersiz. Il/Ilce bilgisi eksik."))
        );

        var httpClient = new HttpClient();
        var adapter = CreateArasAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Aras Kargo");
    }

    [Fact]
    public async Task ArasKargo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange — server returns 503 (circuit breaker will also kick in after retries)
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable")
        );

        var httpClient = new HttpClient();
        var adapter = CreateArasAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ArasKargo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments/*/label").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateArasAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("ARAS-SH-001");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.Data.Length.Should().BeGreaterThan(0);
        label.FileName.Should().Contain("aras-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. SURAT KARGO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SuratKargo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v2/cargo/create").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonOk("SURAT111222333", "SURAT-SH-001"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateSuratAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("SURAT111222333");
        result.ShipmentId.Should().Be("SURAT-SH-001");
    }

    [Fact]
    public async Task SuratKargo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v2/cargo/create").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonError("Adres bilgileri eksik"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateSuratAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Surat Kargo");
    }

    [Fact]
    public async Task SuratKargo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v2/cargo/create").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable")
        );

        var httpClient = new HttpClient();
        var adapter = CreateSuratAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SuratKargo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v2/cargo/*/label").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateSuratAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("SURAT-SH-001");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("surat-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. MNG KARGO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MngKargo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonOk("MNG444555666", "MNG-SH-001"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateMngAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("MNG444555666");
        result.ShipmentId.Should().Be("MNG-SH-001");
    }

    [Fact]
    public async Task MngKargo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonError("Alici il/ilce bilgisi hatali"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateMngAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("MNG Kargo");
    }

    [Fact]
    public async Task MngKargo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable")
        );

        var httpClient = new HttpClient();
        var adapter = CreateMngAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MngKargo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments/*/label").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateMngAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("MNG-SH-001");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("mng-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. PTT KARGO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PttKargo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/ptt-shipment").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(PttSoapOk("PTT123456789"))
        );

        var httpClient = new HttpClient();
        var adapter = CreatePttAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("PTT123456789");
        result.ShipmentId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PttKargo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/ptt-shipment").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapFault("Alici adresi hatali — il/ilce bilgisi bulunamadi"))
        );

        var httpClient = new HttpClient();
        var adapter = CreatePttAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PttKargo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/ptt-shipment").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error")
        );

        var httpClient = new HttpClient();
        var adapter = CreatePttAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PttKargo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/soap/ptt-shipment").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(PttSoapLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreatePttAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("PTT123456789");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("ptt-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 6. HEPSIJET
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HepsiJet_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonOk("HJ777888999", "HJ-SH-001"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateHepsiJetAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("HJ777888999");
        result.ShipmentId.Should().Be("HJ-SH-001");
    }

    [Fact]
    public async Task HepsiJet_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonError("Gonderi adresi eksik veya hatali"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateHepsiJetAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HepsiJet");
    }

    [Fact]
    public async Task HepsiJet_ApiDown_ReturnsGracefulFail()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable")
        );

        var httpClient = new HttpClient();
        var adapter = CreateHepsiJetAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HepsiJet_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments/*/label").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateHepsiJetAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("HJ-SH-001");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("hepsijet-label-");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 7. SENDEO
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sendeo_HappyPath_CreateShipment_ReturnsTrackingNumber()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonOk("SND000111222", "SND-SH-001"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateSendeoAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("SND000111222");
        result.ShipmentId.Should().Be("SND-SH-001");
    }

    [Fact]
    public async Task Sendeo_InvalidAddress_ReturnsGracefulError()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonError("Alici sehir bilgisi hatali"))
        );

        var httpClient = new HttpClient();
        var adapter = CreateSendeoAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateInvalidAddressRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Sendeo");
    }

    [Fact]
    public async Task Sendeo_ApiDown_ReturnsGracefulFail()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable")
        );

        var httpClient = new HttpClient();
        var adapter = CreateSendeoAdapter(httpClient);

        // Act
        var result = await adapter.CreateShipmentAsync(CreateValidRequest());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Sendeo_LabelFormat_ReturnsPdf()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/api/v1/shipments/*/label").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonLabelOk())
        );

        var httpClient = new HttpClient();
        var adapter = CreateSendeoAdapter(httpClient);

        // Act
        var label = await adapter.GetShipmentLabelAsync("SND-SH-001");

        // Assert
        label.Should().NotBeNull();
        label.Format.Should().Be(LabelFormat.Pdf);
        label.Data.Should().NotBeEmpty();
        label.FileName.Should().Contain("sendeo-label-");
    }
}
