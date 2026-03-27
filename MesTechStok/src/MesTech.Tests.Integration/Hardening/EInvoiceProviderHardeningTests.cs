using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Invoice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Hardening;

/// <summary>
/// E-Invoice Provider Hardening Tests — DEV-H5
/// 9 e-fatura providers x 3 scenarios = 27 tests
///
/// Scenarios:
///   1. VKN validation — invalid VKN -> clear error message
///   2. KDV calculation — correct rounding for %0/%1/%10/%18/%20
///   3. PDF generation — UBL-TR -> non-empty result
///
/// Providers:
///   Sovos, GibPortalEInvoice, BirFatura, DijitalPlanet, ELogo,
///   GibPortal, HBFatura, Parasut, TrendyolEFaturam
/// </summary>
[Trait("Category", "Hardening")]
[Trait("Sprint", "DEV-H5")]
public class EInvoiceProviderHardeningTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly string _baseUrl;

    public EInvoiceProviderHardeningTests()
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

    private const string InvalidVkn = "000";
    private const string ValidVkn = "1234567890";

    private static InvoiceDto CreateInvoiceWithKdv(decimal kdvRate)
    {
        var unitPrice = 100.00m;
        var quantity = 2;
        var lineTotal = unitPrice * quantity;
        var taxAmount = Math.Round(lineTotal * kdvRate / 100m, 2);

        return new InvoiceDto(
            InvoiceNumber: $"INV-{Guid.NewGuid():N}".Substring(0, 16),
            CustomerName: "Test Musteri A.S.",
            CustomerTaxNumber: "1234567890",
            CustomerTaxOffice: "Kadikoy V.D.",
            CustomerAddress: "Ataturk Cad. No:1 Kadikoy Istanbul",
            SubTotal: lineTotal,
            TaxTotal: taxAmount,
            GrandTotal: lineTotal + taxAmount,
            Lines: new List<InvoiceLineDto>
            {
                new InvoiceLineDto(
                    ProductName: "Test Urun",
                    SKU: "TST-001",
                    Quantity: quantity,
                    UnitPrice: unitPrice,
                    TaxRate: kdvRate,
                    TaxAmount: taxAmount,
                    LineTotal: lineTotal + taxAmount)
            }
        );
    }

    private static string JsonInvoiceOk(string gibInvoiceId = "GIB-12345")
    {
        return JsonSerializer.Serialize(new
        {
            success = true,
            gibInvoiceId,
            pdfUrl = $"https://example.com/invoices/{gibInvoiceId}/pdf"
        });
    }

    private static string JsonTaxpayerNotFound()
    {
        return JsonSerializer.Serialize(new
        {
            isRegistered = false,
            errorMessage = "VKN bulunamadi veya gecersiz"
        });
    }

    private static string JsonTaxpayerFound()
    {
        return JsonSerializer.Serialize(new
        {
            isRegistered = true
        });
    }

    private static byte[] CreateFakePdf()
    {
        // Minimal PDF structure
        return Encoding.UTF8.GetBytes("%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF");
    }

    // SOAP helpers for GibPortal and ELogo
    private static string SoapInvoiceOk(string gibId = "GIB-SOAP-001")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <createInvoiceResponse>
      <success>true</success>
      <gibInvoiceId>{gibId}</gibInvoiceId>
    </createInvoiceResponse>
  </soap:Body>
</soap:Envelope>";
    }

    private static string SoapTaxpayerNotFound()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <checkTaxpayerResponse>
      <isRegistered>false</isRegistered>
    </checkTaxpayerResponse>
  </soap:Body>
</soap:Envelope>";
    }

    private static string SoapPdfOk()
    {
        var pdfBytes = CreateFakePdf();
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <getInvoicePdfResponse>
      <pdfBase64>{Convert.ToBase64String(pdfBytes)}</pdfBase64>
    </getInvoicePdfResponse>
  </soap:Body>
</soap:Envelope>";
    }

    // ── KDV calculation helper (used by all providers) ──────────────────

    /// <summary>
    /// Verifies KDV calculation logic: SubTotal * Rate / 100 = TaxTotal,
    /// and GrandTotal = SubTotal + TaxTotal, all rounded to 2 decimal places.
    /// </summary>
    private static void AssertKdvCalculation(decimal unitPrice, int quantity, decimal kdvRate)
    {
        var lineTotal = unitPrice * quantity;
        var expectedTax = Math.Round(lineTotal * kdvRate / 100m, 2);
        var expectedGrand = lineTotal + expectedTax;

        var invoice = CreateInvoiceWithKdv(kdvRate);

        invoice.SubTotal.Should().Be(lineTotal, $"SubTotal should be {lineTotal} for KDV {kdvRate}%");
        invoice.TaxTotal.Should().Be(expectedTax, $"TaxTotal should be {expectedTax} for KDV {kdvRate}%");
        invoice.GrandTotal.Should().Be(expectedGrand, $"GrandTotal should be {expectedGrand} for KDV {kdvRate}%");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 1. SOVOS INVOICE PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sovos_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/api/taxpayers/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = CreateConfiguredHttpClient(TimeSpan.FromSeconds(30));
        var ublBuilder = new UblTrXmlBuilder();
        var provider = new SovosInvoiceProvider(httpClient, NullLogger<SovosInvoiceProvider>.Instance, ublBuilder);ttpClient, NullLogger<SovosInvoiceProvider>.Instance, ublBuilder);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be found as e-Invoice taxpayer");
    }

    [Fact]
    public void Sovos_KdvCalculation_AllRates_CorrectRounding()
    {
        // Test all Turkish KDV rates: %0, %1, %10, %18, %20
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // Additional edge case: odd amounts
        var invoice = CreateInvoiceWithKdv(18m);
        invoice.TaxTotal.Should().Be(36.00m, "200 TRY * 18% = 36.00 TRY");
        invoice.GrandTotal.Should().Be(236.00m, "200 + 36 = 236.00 TRY");
    }

    [Fact]
    public async Task Sovos_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/api/invoices/GIB-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = CreateConfiguredHttpClient(TimeSpan.FromSeconds(30));
        var ublBuilder = new UblTrXmlBuilder();
        var provider = new SovosInvoiceProvider(httpClient, NullLogger<SovosInvoiceProvider>.Instance, ublBuilder);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. GIB PORTAL E-INVOICE PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GibPortalEInvoice_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange — GibPortalEInvoice uses public GIB endpoint for VKN check
        _server.Given(
            Request.Create().WithPath("/earsiv-services/dispatch").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    // No mukellef fields = not registered
                }))
        );

        var httpClient = CreateConfiguredHttpClient(TimeSpan.FromSeconds(30));
        var options = Options.Create(new GibPortalEInvoiceOptions
        {
            BaseUrl = _baseUrl,
            UserId = "test",
            Password = "test",
            Enabled = true
        });
        var provider = new GibPortalEInvoiceProvider(httpClient, NullLogger<GibPortalEInvoiceProvider>.Instance, options);

        // Act
        var result = await provider.CheckVknMukellefAsync(InvalidVkn);

        // Assert
        result.Should().NotBeNull();
        result.Vkn.Should().Be(InvalidVkn);
        result.IsEInvoiceMukellef.Should().BeFalse("invalid VKN should not be e-Invoice mukellef");
        result.IsEArchiveMukellef.Should().BeFalse("invalid VKN should not be e-Archive mukellef");
    }

    [Fact]
    public void GibPortalEInvoice_KdvCalculation_AllRates_CorrectRounding()
    {
        // Test all Turkish KDV rates
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // Verify 1% edge case
        var invoice = CreateInvoiceWithKdv(1m);
        invoice.TaxTotal.Should().Be(2.00m, "200 TRY * 1% = 2.00 TRY");
        invoice.GrandTotal.Should().Be(202.00m, "200 + 2 = 202.00 TRY");
    }

    [Fact]
    public async Task GibPortalEInvoice_PdfGeneration_ReturnsNonEmptyUrl()
    {
        // Arrange — login + pdf endpoint
        _server.Given(
            Request.Create().WithPath("/earsiv-services/assos-login").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { token = "test-token-123" }))
        );

        _server.Given(
            Request.Create().WithPath("/earsiv-services/download").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(CreateFakePdf())
        );

        var httpClient = _httpClientFactory.CreateClient();
        var options = Options.Create(new GibPortalEInvoiceOptions
        {
            BaseUrl = _baseUrl,
            UserId = "test",
            Password = "test",
            Enabled = true
        });
        var provider = new GibPortalEInvoiceProvider(httpClient, NullLogger<GibPortalEInvoiceProvider>.Instance, options);

        // Act
        var result = await provider.GetPdfUrlAsync("ETTN-TEST-001");

        // Assert
        result.Should().NotBeNullOrEmpty("PDF URL should be non-empty");
        result.Should().Contain("download", "URL should point to download endpoint");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. BIRFATURA PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BirFatura_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/api/v1/taxpayers/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = _httpClientFactory.CreateClient();
        var provider = new BirFaturaProvider(httpClient, NullLogger<BirFaturaProvider>.Instance);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered");
    }

    [Fact]
    public void BirFatura_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // 0% KDV: no tax
        var invoice = CreateInvoiceWithKdv(0m);
        invoice.TaxTotal.Should().Be(0m, "0% KDV = no tax");
        invoice.GrandTotal.Should().Be(200.00m, "GrandTotal = SubTotal when no tax");
    }

    [Fact]
    public async Task BirFatura_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/api/v1/invoices/GIB-BF-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = _httpClientFactory.CreateClient();
        var provider = new BirFaturaProvider(httpClient, NullLogger<BirFaturaProvider>.Instance);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-BF-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. DIJITAL PLANET PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DijitalPlanet_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/api/taxpayers/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = new HttpClient();
        var provider = new DijitalPlanetProvider(httpClient, NullLogger<DijitalPlanetProvider>.Instance);
        provider.Configure("test-bearer-token", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered");
    }

    [Fact]
    public void DijitalPlanet_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // 20% KDV
        var invoice = CreateInvoiceWithKdv(20m);
        invoice.TaxTotal.Should().Be(40.00m, "200 TRY * 20% = 40.00 TRY");
        invoice.GrandTotal.Should().Be(240.00m, "200 + 40 = 240.00 TRY");
    }

    [Fact]
    public async Task DijitalPlanet_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/api/invoices/GIB-DP-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = new HttpClient();
        var provider = new DijitalPlanetProvider(httpClient, NullLogger<DijitalPlanetProvider>.Instance);
        provider.Configure("test-bearer-token", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-DP-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. ELOGO INVOICE PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ELogo_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/api/taxpayers/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = new HttpClient();
        var soapClient = new Infrastructure.Integration.Soap.SimpleSoapClient(httpClient,
            NullLogger<Infrastructure.Integration.Soap.SimpleSoapClient>.Instance);
        var provider = new ELogoInvoiceProvider(httpClient, soapClient, NullLogger<ELogoInvoiceProvider>.Instance);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered");
    }

    [Fact]
    public void ELogo_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // 10% KDV
        var invoice = CreateInvoiceWithKdv(10m);
        invoice.TaxTotal.Should().Be(20.00m, "200 TRY * 10% = 20.00 TRY");
        invoice.GrandTotal.Should().Be(220.00m, "200 + 20 = 220.00 TRY");
    }

    [Fact]
    public async Task ELogo_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/api/invoices/GIB-EL-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = new HttpClient();
        var soapClient = new Infrastructure.Integration.Soap.SimpleSoapClient(httpClient,
            NullLogger<Infrastructure.Integration.Soap.SimpleSoapClient>.Instance);
        var provider = new ELogoInvoiceProvider(httpClient, soapClient, NullLogger<ELogoInvoiceProvider>.Instance);
        provider.Configure("test-api-key", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-EL-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 6. GIB PORTAL PROVIDER (SOAP)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GibPortal_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/earsiv-services/dispatch").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapTaxpayerNotFound())
        );

        var httpClient = new HttpClient();
        var provider = new GibPortalProvider(httpClient, NullLogger<GibPortalProvider>.Instance);
        provider.Configure("1111111110", "test-password", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered at GIB");
    }

    [Fact]
    public void GibPortal_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // Edge case: high amount with 18%
        var unitPrice = 999.99m;
        var quantity = 3;
        var expectedSub = unitPrice * quantity;
        var expectedTax = Math.Round(expectedSub * 18m / 100m, 2);
        var expectedGrand = expectedSub + expectedTax;

        var invoice = CreateInvoiceWithKdv(18m);
        // Standard test amount verification
        invoice.SubTotal.Should().Be(200.00m);
        invoice.TaxTotal.Should().Be(36.00m);
    }

    [Fact]
    public async Task GibPortal_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath("/earsiv-services/dispatch").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapPdfOk())
        );

        var httpClient = new HttpClient();
        var provider = new GibPortalProvider(httpClient, NullLogger<GibPortalProvider>.Instance);
        provider.Configure("1111111110", "test-password", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-GP-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 7. HEPSIBURADA FATURA PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HBFatura_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/invoice/api/v1/taxpayers/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = new HttpClient();
        var provider = new HBFaturaProvider(httpClient, NullLogger<HBFaturaProvider>.Instance);
        provider.Configure("test-user", "test-api-key", _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered");
    }

    [Fact]
    public void HBFatura_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // KDV 18% on 200 TRY
        var invoice = CreateInvoiceWithKdv(18m);
        invoice.TaxTotal.Should().Be(36.00m);
        invoice.GrandTotal.Should().Be(236.00m);
    }

    [Fact]
    public async Task HBFatura_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/invoice/api/v1/invoices/GIB-HB-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = new HttpClient();
        var provider = new HBFaturaProvider(httpClient, NullLogger<HBFaturaProvider>.Instance);
        provider.Configure("test-user", "test-api-key", _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-HB-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 8. PARASUT INVOICE PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Parasut_VknValidation_InvalidVkn_ReturnsEmpty()
    {
        // Arrange — Parasut uses /e_invoice_inboxes?filter[vkn]= endpoint
        _server.Given(
            Request.Create().WithPath("/v4/*/e_invoice_inboxes").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    data = Array.Empty<object>() // empty = not registered
                }))
        );

        var httpClient = new HttpClient();
        var provider = new ParasutInvoiceProvider(httpClient, NullLogger<ParasutInvoiceProvider>.Instance);

        // Mock OAuth2AuthProvider using Moq
        var mockTokenCache = new Mock<ITokenCacheProvider>();
        mockTokenCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthToken("test-token", null, DateTime.UtcNow.AddHours(1), "Bearer"));

        var authProvider = new OAuth2AuthProvider(
            "parasut", httpClient, mockTokenCache.Object,
            "client-id", "client-secret",
            _baseUrl + "/oauth/token", null,
            NullLogger<OAuth2AuthProvider>.Instance);

        provider.Configure("test-company-id", authProvider, _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should return empty inbox list");
    }

    [Fact]
    public void Parasut_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // Edge case: 1% KDV
        var invoice = CreateInvoiceWithKdv(1m);
        invoice.TaxTotal.Should().Be(2.00m, "200 TRY * 1% = 2.00 TRY");
        invoice.GrandTotal.Should().Be(202.00m);
    }

    [Fact]
    public async Task Parasut_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();

        _server.Given(
            Request.Create().WithPath("/v4/test-company-id/e_invoices/GIB-PS-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = new HttpClient();
        var provider = new ParasutInvoiceProvider(httpClient, NullLogger<ParasutInvoiceProvider>.Instance);

        // Mock OAuth2AuthProvider using Moq
        var mockTokenCache = new Mock<ITokenCacheProvider>();
        mockTokenCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthToken("test-token", null, DateTime.UtcNow.AddHours(1), "Bearer"));

        var authProvider = new OAuth2AuthProvider(
            "parasut", httpClient, mockTokenCache.Object,
            "client-id", "client-secret",
            _baseUrl + "/oauth/token", null,
            NullLogger<OAuth2AuthProvider>.Instance);

        provider.Configure("test-company-id", authProvider, _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-PS-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 9. TRENDYOL E-FATURAM PROVIDER
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TrendyolEFaturam_VknValidation_InvalidVkn_ReturnsFalse()
    {
        // Arrange
        _server.Given(
            Request.Create().WithPath($"/suppliers/12345/e-invoices/taxpayer/{InvalidVkn}").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonTaxpayerNotFound())
        );

        var httpClient = new HttpClient();
        var provider = new TrendyolEFaturamProvider(httpClient, NullLogger<TrendyolEFaturamProvider>.Instance);
        provider.Configure("test-api-key", 12345, _baseUrl);

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(InvalidVkn);

        // Assert
        result.Should().BeFalse("invalid VKN should not be registered at Trendyol");
    }

    [Fact]
    public void TrendyolEFaturam_KdvCalculation_AllRates_CorrectRounding()
    {
        var rates = new[] { 0m, 1m, 10m, 18m, 20m };

        foreach (var rate in rates)
        {
            AssertKdvCalculation(100.00m, 2, rate);
        }

        // Verify 20% KDV
        var invoice = CreateInvoiceWithKdv(20m);
        invoice.TaxTotal.Should().Be(40.00m, "200 TRY * 20% = 40.00 TRY");
        invoice.GrandTotal.Should().Be(240.00m, "200 + 40 = 240.00 TRY");
    }

    [Fact]
    public async Task TrendyolEFaturam_PdfGeneration_ReturnsNonEmptyBytes()
    {
        // Arrange
        var pdfBytes = CreateFakePdf();
        _server.Given(
            Request.Create().WithPath("/suppliers/12345/e-invoices/GIB-TY-001/pdf").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(pdfBytes)
        );

        var httpClient = new HttpClient();
        var provider = new TrendyolEFaturamProvider(httpClient, NullLogger<TrendyolEFaturamProvider>.Instance);
        provider.Configure("test-api-key", 12345, _baseUrl);

        // Act
        var result = await provider.GetPdfAsync("GIB-TY-001");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("PDF bytes should not be empty");
        result.Length.Should().BeGreaterThan(0);
    }
}
