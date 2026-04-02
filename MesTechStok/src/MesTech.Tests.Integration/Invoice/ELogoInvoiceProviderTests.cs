using System.Net;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Infrastructure.Integration.Soap;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// e-Logo invoice provider contract tests — REST+SOAP hybrid.
/// SOAP tests: WireMock serves SOAP envelope XML at /soap/invoice.
/// REST tests: WireMock serves JSON at /api/... endpoints.
/// 18 WireMock tests covering IInvoiceProvider + IBulkInvoiceCapable + IIncomingInvoiceCapable + IKontorCapable.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ELogo")]
public class ELogoInvoiceProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<ELogoInvoiceProvider> _logger;

    private const string TestApiKey = "SET_VIA_ENVIRONMENT_VARIABLE_OR_USER_SECRETS";
    private const string TestGibInvoiceId = "GIB2026031000001";

    public ELogoInvoiceProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<ELogoInvoiceProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private ELogoInvoiceProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var soapHttpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var soapLogger = new Mock<ILogger<SimpleSoapClient>>().Object;
        var soapClient = new SimpleSoapClient(soapHttpClient, soapLogger);
        return new ELogoInvoiceProvider(httpClient, soapClient, _logger);
    }

    private ELogoInvoiceProvider CreateConfiguredProvider()
    {
        var provider = CreateProvider();
        provider.Configure(TestApiKey, _fixture.BaseUrl);
        return provider;
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

    private static InvoiceCreateRequest CreateTestInvoiceRequest(string number = "INV-2026-001")
    {
        return new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            PlatformOrderId = number,
            Type = InvoiceType.EFatura,
            Customer = new InvoiceCustomerInfo(
                "Test Musteri A.S.", "1234567890", "Kadikoy", "Istanbul, Turkiye", null, null),
            TotalAmount = 1200m,
            Lines = new List<InvoiceCreateLine>
            {
                new("Urun A", "SKU-001", 2, 400m, 20m, null),
                new("Urun B", "SKU-002", 1, 200m, 20m, null)
            }
        };
    }

    private static string BuildSoapSuccessResponse(string gibInvoiceId, string? pdfUrl = null)
    {
        var pdfElement = pdfUrl != null
            ? $"<pdfUrl>{pdfUrl}</pdfUrl>"
            : "<pdfUrl />";
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <createInvoiceResponse>
                        <gibInvoiceId>{gibInvoiceId}</gibInvoiceId>
                        {pdfElement}
                    </createInvoiceResponse>
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
            .Given(Request.Create().WithPath("/soap/invoice").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapSuccessResponse("GIB2026031000001", "https://elogo.com.tr/pdf/GIB2026031000001")));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000001");
        result.PdfUrl.Should().Contain("GIB2026031000001");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. CreateEArsiv — SOAP success ════

    [Fact]
    public async Task CreateEArsiv_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/soap/invoice").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(BuildSoapSuccessResponse("GIB2026031000002", "https://elogo.com.tr/pdf/GIB2026031000002")));

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
            .Given(Request.Create().WithPath("/soap/invoice").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <createDispatchResponse>
                                <gibInvoiceId>IRS2026031000001</gibInvoiceId>
                                <pdfUrl />
                            </createDispatchResponse>
                        </soap:Body>
                    </soap:Envelope>"));

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
            .Given(Request.Create().WithPath("/soap/invoice").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>
                            <soap:Fault>
                                <faultcode>Server</faultcode>
                                <faultstring>UBL validation failed</faultstring>
                            </soap:Fault>
                        </soap:Body>
                    </soap:Envelope>"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert — SimpleSoapClient throws HttpRequestException on non-success status
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 5. CheckStatus — REST returns status ════

    [Fact]
    public async Task CheckStatus_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Accepted"",
                    ""acceptedAt"": ""2026-03-10T14:30:00Z"",
                    ""errorMessage"": null
                }"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Accepted");
        result.AcceptedAt.Should().NotBeNull();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 6. CheckStatus — Server error returns error ════

    [Fact]
    public async Task CheckStatus_ServerError_ReturnsError()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Error");
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 7. GetPdf — REST returns PDF bytes ════

    [Fact]
    public async Task GetPdf_ReturnsPdfBytes()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF- header

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/pdf").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(fakePdf));

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
            .Given(Request.Create()
                .WithPath($"/api/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": true, ""title"": ""Test Musteri A.S.""}"));

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
            .Given(Request.Create()
                .WithPath($"/api/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": false}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 10. CancelInvoice — REST success ════

    [Fact]
    public async Task CancelInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ════ 11. CancelInvoice — Server error ════

    [Fact]
    public async Task CancelInvoice_ServerError_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/{TestGibInvoiceId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 12. CreateBulkInvoice — REST all success ════

    [Fact]
    public async Task CreateBulkInvoice_Success_ReturnsAllResults()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("BULK-001"), CreateTestInvoiceRequest("BULK-002") };

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing/bulk").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""results"": [
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-001"", ""errorMessage"": null },
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-002"", ""errorMessage"": null }
                    ]
                }"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Results.Should().HaveCount(2);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].GibInvoiceId.Should().Be("GIB-BULK-001");
        result.Results[1].Success.Should().BeTrue();
        result.Results[1].GibInvoiceId.Should().Be("GIB-BULK-002");
    }

    // ════ 13. CreateBulkInvoice — REST partial failure ════

    [Fact]
    public async Task CreateBulkInvoice_PartialFailure_ReturnsCorrectCounts()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("BULK-003"), CreateTestInvoiceRequest("BULK-004") };

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing/bulk").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""results"": [
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-003"", ""errorMessage"": null },
                        { ""success"": false, ""gibInvoiceId"": null, ""errorMessage"": ""Invalid tax number"" }
                    ]
                }"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Results.Should().HaveCount(2);
        result.Results[0].Success.Should().BeTrue();
        result.Results[1].Success.Should().BeFalse();
        result.Results[1].ErrorMessage.Should().Be("Invalid tax number");
    }

    // ════ 14. GetIncomingInvoices — REST returns list ════

    [Fact]
    public async Task GetIncomingInvoices_ReturnsList()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 10);

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/api/invoices/incoming")
                .WithParam("from", "2026-03-01")
                .WithParam("to", "2026-03-10")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""invoices"": [
                        {
                            ""gibInvoiceId"": ""GIB-IN-001"",
                            ""invoiceNumber"": ""INV-GELEN-001"",
                            ""senderName"": ""Tedarikci A.S."",
                            ""senderTaxNumber"": ""9876543210"",
                            ""amount"": 5000.00,
                            ""invoiceDate"": ""2026-03-05T00:00:00Z"",
                            ""pdfUrl"": ""https://elogo.com.tr/pdf/GIB-IN-001"",
                            ""status"": ""Accepted""
                        }
                    ]
                }"));

        // Act
        var result = await provider.GetIncomingInvoicesAsync(from, to);

        // Assert
        result.Should().HaveCount(1);
        result[0].GibInvoiceId.Should().Be("GIB-IN-001");
        result[0].InvoiceNumber.Should().Be("INV-GELEN-001");
        result[0].SenderName.Should().Be("Tedarikci A.S.");
        result[0].SenderTaxNumber.Should().Be("9876543210");
        result[0].GrandTotal.Should().Be(5000.00m);
        result[0].PdfUrl.Should().Contain("GIB-IN-001");
        result[0].Status.Should().Be(InvoiceStatus.Accepted);
    }

    // ════ 15. AcceptInvoice — REST success ════

    [Fact]
    public async Task AcceptInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/incoming/{TestGibInvoiceId}/accept")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.AcceptInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 16. RejectInvoice — REST success ════

    [Fact]
    public async Task RejectInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/api/invoices/incoming/{TestGibInvoiceId}/reject")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.RejectInvoiceAsync(TestGibInvoiceId, "Yanlis tutar");

        // Assert
        result.Should().BeTrue();
    }

    // ════ 17. GetKontorBalance — REST returns balance ════

    [Fact]
    public async Task GetKontorBalance_ReturnsBalance()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/account/kontor").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""remaining"": 280,
                    ""total"": 500,
                    ""expiresAt"": ""2026-12-31T23:59:59Z""
                }"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(280);
        result.TotalKontor.Should().Be(500);
        result.ExpiresAt.Should().NotBeNull();
        result.ProviderName.Should().Be("e-Logo");
    }

    // ════ 18. NotConfigured — throws InvalidOperationException ════

    [Fact]
    public async Task NotConfigured_ThrowsInvalidOperation()
    {
        // Arrange — do NOT call Configure()
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.CreateEFaturaAsync(CreateTestInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }
}
