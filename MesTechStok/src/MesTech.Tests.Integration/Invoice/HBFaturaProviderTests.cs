using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// Hepsiburada Fatura provider contract tests — REST JSON API + Basic Auth (username:apiKey).
/// 17 WireMock tests covering IInvoiceProvider + IBulkInvoiceCapable + IKontorCapable.
/// URL pattern: /invoice/api/v1/...
/// Auth: Authorization: Basic Base64(username:apiKey)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "HBFatura")]
public class HBFaturaProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;

    private const string TestUsername = "test-user";
    private const string TestApiKey = "test-api-key-hb";
    private const string TestGibInvoiceId = "GIB-HB-001";
    private const string ApiBase = "/invoice/api/v1";

    public HBFaturaProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private HBFaturaProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new HBFaturaProvider(httpClient, NullLogger<HBFaturaProvider>.Instance);
    }

    private HBFaturaProvider CreateConfiguredProvider()
    {
        var provider = CreateProvider();
        provider.Configure(TestUsername, TestApiKey, _fixture.BaseUrl);
        return provider;
    }

    private static InvoiceDto CreateTestInvoice(string number = "INV-HB-2026-001")
    {
        return new InvoiceDto(
            InvoiceNumber: number,
            CustomerName: "HB Test Musteri A.S.",
            CustomerTaxNumber: "1234567890",
            CustomerTaxOffice: "Umraniye",
            CustomerAddress: "Istanbul, Turkiye",
            SubTotal: 1000m,
            TaxTotal: 200m,
            GrandTotal: 1200m,
            Lines: new List<InvoiceLineDto>
            {
                new("Urun A", "HB-SKU-001", 2, 400m, 20m, 160m, 960m),
                new("Urun B", "HB-SKU-002", 1, 200m, 20m, 40m, 240m)
            }
        );
    }

    private static InvoiceCreateRequest CreateTestInvoiceRequest(string platformOrderId = "HB-ORDER-001")
    {
        return new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Platform = PlatformType.Hepsiburada,
            PlatformOrderId = platformOrderId,
            Type = InvoiceType.EFatura,
            Customer = new InvoiceCustomerInfo(
                "HB Test Musteri A.S.", "1234567890", "Umraniye", "Istanbul, Turkiye", null, null),
            TotalAmount = 1200m,
            Lines = new List<InvoiceCreateLine>
            {
                new("Urun A", "HB-SKU-001", 2, 400m, 20m, null),
                new("Urun B", "HB-SKU-002", 1, 200m, 20m, null)
            }
        };
    }

    // ════ 1. Configure — Basic Auth header set ════

    [Fact]
    public void Configure_SetsBasicAuthHeader()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new HBFaturaProvider(httpClient, NullLogger<HBFaturaProvider>.Instance);

        // Act
        provider.Configure(TestUsername, TestApiKey, _fixture.BaseUrl);

        // Assert
        var authHeader = httpClient.DefaultRequestHeaders.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Basic");

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter!));
        decoded.Should().Be($"{TestUsername}:{TestApiKey}");
    }

    // ════ 2. EnsureConfigured — throws if not configured ════

    [Fact]
    public async Task EnsureConfigured_ThrowsIfNotConfigured()
    {
        // Arrange — do NOT call Configure()
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.CreateEFaturaAsync(CreateTestInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    // ════ 3. CreateEFatura — Success returns GibInvoiceId ════

    [Fact]
    public async Task CreateEFatura_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""gibInvoiceId"":""GIB-HB-001"",""pdfUrl"":""https://example.com/pdf/001.pdf""}"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB-HB-001");
        result.PdfUrl.Should().Contain("001.pdf");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 4. CreateEFatura — HTTP error returns failure ════

    [Fact]
    public async Task CreateEFatura_HttpError_ReturnsFailure()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"":""Internal Server Error""}"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.GibInvoiceId.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 5. CreateEArsiv — Success returns GibInvoiceId ════

    [Fact]
    public async Task CreateEArsiv_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/earsiv").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""gibInvoiceId"":""GIB-HB-ARSIV-001"",""pdfUrl"":""https://example.com/pdf/arsiv-001.pdf""}"));

        // Act
        var result = await provider.CreateEArsivAsync(CreateTestInvoice("ARSIV-HB-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB-HB-ARSIV-001");
    }

    // ════ 6. CreateEIrsaliye — Success returns GibInvoiceId ════

    [Fact]
    public async Task CreateEIrsaliye_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/eirsaliye").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""gibInvoiceId"":""IRS-HB-001"",""pdfUrl"":null}"));

        // Act
        var result = await provider.CreateEIrsaliyeAsync(CreateTestInvoice("IRS-HB-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("IRS-HB-001");
    }

    // ════ 7. CheckStatus — Success returns status ════

    [Fact]
    public async Task CheckStatus_Success_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""Accepted"",""acceptedAt"":""2026-03-10T10:00:00Z"",""errorMessage"":null}"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Accepted");
        result.AcceptedAt.Should().NotBeNull();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 8. CheckStatus — HTTP error returns Error status ════

    [Fact]
    public async Task CheckStatus_HttpError_ReturnsError()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody(@"{""error"":""Service Unavailable""}"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Error");
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 9. GetPdf — Success returns byte array ════

    [Fact]
    public async Task GetPdf_Success_ReturnsByteArray()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF- header

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/pdf").UsingGet())
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

    // ════ 10. IsEInvoiceTaxpayer — Registered returns true ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_Registered_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "1234567890";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"":true}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 11. IsEInvoiceTaxpayer — Not registered returns false ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_NotRegistered_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "9999999999";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"":false}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 12. CancelInvoice — Success returns true ════

    [Fact]
    public async Task CancelInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ════ 13. CancelInvoice — HTTP error returns false ════

    [Fact]
    public async Task CancelInvoice_HttpError_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithBody(@"{""error"":""Unprocessable Entity — invoice already cancelled""}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 14. CreateBulkInvoice — Success returns results ════

    [Fact]
    public async Task CreateBulkInvoice_Success_ReturnsResults()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("HB-BULK-001"), CreateTestInvoiceRequest("HB-BULK-002") };

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/bulk").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""results"":[{""success"":true,""gibInvoiceId"":""GIB-HB-BULK-001"",""errorMessage"":null},{""success"":true,""gibInvoiceId"":""GIB-HB-BULK-002"",""errorMessage"":null}]}"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Results.Should().HaveCount(2);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].GibInvoiceId.Should().Be("GIB-HB-BULK-001");
        result.Results[1].Success.Should().BeTrue();
        result.Results[1].GibInvoiceId.Should().Be("GIB-HB-BULK-002");
    }

    // ════ 15. CreateBulkInvoice — HTTP error returns all failures ════

    [Fact]
    public async Task CreateBulkInvoice_HttpError_ReturnsFailures()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("HB-BULK-003"), CreateTestInvoiceRequest("HB-BULK-004") };

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/bulk").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"":""Internal Server Error""}"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.Results.Should().HaveCount(2);
        result.Results[0].Success.Should().BeFalse();
        result.Results[1].Success.Should().BeFalse();
    }

    // ════ 16. GetKontorBalance — Success returns balance (BONUS) ════

    [Fact]
    public async Task GetKontorBalance_Success_ReturnsBalance()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/kontor/balance").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""remainingKontor"":500,""totalKontor"":1000,""expiresAt"":""2026-12-31T00:00:00Z""}"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(500);
        result.TotalKontor.Should().Be(1000);
        result.ExpiresAt.Should().NotBeNull();
        result.ProviderName.Should().Be("Hepsiburada Fatura");
    }

    // ════ 17. GetKontorBalance — HTTP error returns zero balance (BONUS) ════

    [Fact]
    public async Task GetKontorBalance_HttpError_ReturnsZeroBalance()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/kontor/balance").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody(@"{""error"":""Service Unavailable""}"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(0);
        result.TotalKontor.Should().Be(0);
        result.ExpiresAt.Should().BeNull();
        result.ProviderName.Should().Be("Hepsiburada Fatura");
    }
}
