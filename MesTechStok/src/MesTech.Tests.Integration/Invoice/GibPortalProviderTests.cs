using System.Net;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// GIB Portal invoice provider contract tests — pure SOAP/XML via HttpClient.
/// All operations go through direct SOAP (VKN+Password auth in SOAP Header).
/// WireMock serves SOAP envelope XML at /earsiv-services/dispatch.
/// 12 WireMock tests covering IInvoiceProvider (base only).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "GibPortal")]
public class GibPortalProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<GibPortalProvider> _logger;

    private const string TestGibInvoiceId = "GIB2026031000001";
    private const string SoapPath = "/earsiv-services/dispatch";
    private const string TestVkn = "1234567890";
    private const string TestPassword = "test-pass-123";

    public GibPortalProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<GibPortalProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private GibPortalProvider CreateConfiguredProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var provider = new GibPortalProvider(httpClient, _logger);
        provider.Configure(TestVkn, TestPassword, _fixture.BaseUrl);
        return provider;
    }

    private GibPortalProvider CreateProvider()
    {
        var httpClient = new HttpClient();
        return new GibPortalProvider(httpClient, _logger);
    }

    private GibPortalProvider CreateUnconfiguredProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new GibPortalProvider(httpClient, _logger);
    }

    private static InvoiceDto CreateTestInvoice(string number = "INV-2026-001")
    {
        return new InvoiceDto(
            InvoiceNumber: number,
            CustomerName: "Test Musteri A.S.",
            CustomerTaxNumber: "1234567890",
            CustomerTaxOffice: "Kadikoy",
            CustomerAddress: "Istanbul, Turkiye",
            SubTotal: 1000m,
            TaxTotal: 200m,
            GrandTotal: 1200m,
            Lines: new List<InvoiceLineDto>
            {
                new("Urun A", "SKU-001", 2, 400m, 20m, 160m, 960m),
                new("Urun B", "SKU-002", 1, 200m, 20m, 40m, 240m)
            }
        );
    }

    private static string BuildSoapSuccessResponse(string responseElement, string gibInvoiceId)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <{responseElement} xmlns=""urn:earsiv:services"">
                        <success>true</success>
                        <gibInvoiceId>{gibInvoiceId}</gibInvoiceId>
                    </{responseElement}>
                </soap:Body>
            </soap:Envelope>";
    }

    private static string BuildSoapFaultResponse(string faultString)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <soap:Fault>
                        <faultcode>Server</faultcode>
                        <faultstring>{faultString}</faultstring>
                    </soap:Fault>
                </soap:Body>
            </soap:Envelope>";
    }

    // ════ 1. CreateEFatura — SOAP success ════

    [Fact]
    public async Task CreateEFatura_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapSuccessResponse("createInvoiceResponse", "GIB2026031000001")));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000001");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. CreateEArsiv — SOAP success ════

    [Fact]
    public async Task CreateEArsiv_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapSuccessResponse("createInvoiceResponse", "GIB2026031000002")));

        // Act
        var result = await provider.CreateEArsivAsync(CreateTestInvoice("ARSIV-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000002");
    }

    // ════ 3. CreateEIrsaliye — SOAP success ════

    [Fact]
    public async Task CreateEIrsaliye_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapSuccessResponse("createDispatchResponse", "IRS2026031000001")));

        // Act
        var result = await provider.CreateEIrsaliyeAsync(CreateTestInvoice("IRS-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("IRS2026031000001");
    }

    // ════ 4. CreateEFatura — SOAP fault returns failure ════

    [Fact]
    public async Task CreateEFatura_SoapFault_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapFaultResponse("UBL validation failed")));

        // Act — SendSoapAsync throws HttpRequestException on non-success status
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 5. CheckStatus — SOAP returns status ════

    [Fact]
    public async Task CheckStatus_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <getInvoiceStatusResponse xmlns=""urn:earsiv:services"">
                                <status>Accepted</status>
                                <acceptedAt>2026-03-10T14:30:00Z</acceptedAt>
                                <errorMessage />
                            </getInvoiceStatusResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Accepted");
        result.AcceptedAt.Should().NotBeNull();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    // ════ 6. CheckStatus — SOAP fault returns error ════

    [Fact]
    public async Task CheckStatus_SoapFault_ReturnsError()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapFaultResponse("Invoice not found")));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Error");
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 7. GetPdf — SOAP returns base64 PDF bytes ════

    [Fact]
    public async Task GetPdf_ReturnsPdfBytes()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF- header
        var base64Pdf = Convert.ToBase64String(fakePdf);

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody($@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <getInvoicePdfResponse xmlns=""urn:earsiv:services"">
                                <pdfBase64>{base64Pdf}</pdfBase64>
                            </getInvoicePdfResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.GetPdfAsync(TestGibInvoiceId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
    }

    // ════ 8. IsEInvoiceTaxpayer — Registered ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_True_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "1234567890";

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <checkTaxpayerResponse xmlns=""urn:earsiv:services"">
                                <isRegistered>true</isRegistered>
                                <title>Test Musteri A.S.</title>
                            </checkTaxpayerResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 9. IsEInvoiceTaxpayer — Unregistered ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_False_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "9999999999";

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <checkTaxpayerResponse xmlns=""urn:earsiv:services"">
                                <isRegistered>false</isRegistered>
                            </checkTaxpayerResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 10. CancelInvoice — SOAP success ════

    [Fact]
    public async Task CancelInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody($@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <cancelInvoiceResponse xmlns=""urn:earsiv:services"">
                                <success>true</success>
                                <gibInvoiceId>{TestGibInvoiceId}</gibInvoiceId>
                            </cancelInvoiceResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 11. CancelInvoice — SOAP fault ════

    [Fact]
    public async Task CancelInvoice_SoapFault_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapFaultResponse("Invoice already cancelled")));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 12. NotConfigured — throws InvalidOperationException ════

    [Fact]
    public async Task NotConfigured_ThrowsInvalidOperation()
    {
        // Arrange — do NOT call Configure()
        var provider = CreateUnconfiguredProvider();

        // Act & Assert
        var act = () => provider.CreateEFaturaAsync(CreateTestInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    // ════ 13. Configure_SetsProperties_Correctly ════

    [Fact]
    public void Configure_SetsProperties_Correctly()
    {
        // Arrange
        var provider = CreateProvider();

        // Act — no exception means configure worked
        var act = () => provider.Configure(TestVkn, TestPassword, "https://earsivportal.efatura.gov.tr");

        // Assert
        act.Should().NotThrow();
        provider.ProviderName.Should().Be("GIB Portal");
        provider.Provider.Should().Be(InvoiceProvider.GibPortal);
    }

    // ════ 14. CreateEFatura_HttpError_ReturnsFailure ════

    [Fact]
    public async Task CreateEFatura_HttpError_ReturnsFailure()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 15. CheckStatus_WithAcceptedAt_ParsesDateTime ════

    [Fact]
    public async Task CheckStatus_WithAcceptedAt_ParsesDateTime()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <getInvoiceStatusResponse xmlns=""urn:earsiv:services"">
                                <status>Accepted</status>
                                <acceptedAt>2026-03-10T14:30:00Z</acceptedAt>
                            </getInvoiceStatusResponse>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Accepted");
        result.AcceptedAt.Should().NotBeNull();
        result.AcceptedAt!.Value.Year.Should().Be(2026);
        result.AcceptedAt!.Value.Month.Should().Be(3);
        result.AcceptedAt!.Value.Day.Should().Be(10);
    }

    // ════ 16. GetPdf_ThrowsOnHttpError ════

    [Fact]
    public async Task GetPdf_ThrowsOnHttpError()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath(SoapPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        // Act
        var act = () => provider.GetPdfAsync(TestGibInvoiceId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ════ 17. GetPdf_MissingPdfBase64Element_ThrowsInvalidOperation ════

    [Fact]
    public async Task GetPdf_MissingPdfBase64Element_ThrowsInvalidOperation()
    {
        // Arrange
        _fixture.Server
            .Given(Request.Create()
                .WithPath("/earsiv-services/dispatch")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soapenv:Body>
                        <getInvoicePdfResponse>
                        </getInvoicePdfResponse>
                    </soapenv:Body>
                </soapenv:Envelope>"));

        var provider = CreateConfiguredProvider();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetPdfAsync("GIB-TEST-001"));
    }
}
