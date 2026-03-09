using System.Net;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// Sovos e-Fatura provider contract tests — REST JSON API + Bearer auth.
/// 12 WireMock tests covering all IInvoiceProvider methods + error scenarios.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Sovos")]
public class SovosInvoiceProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<SovosInvoiceProvider> _logger;

    private const string TestApiKey = "test-sovos-api-key-12345";
    private const string TestGibInvoiceId = "GIB2026030900001";

    public SovosInvoiceProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<SovosInvoiceProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ────────────────────────────────────────────────────────

    private SovosInvoiceProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new SovosInvoiceProvider(httpClient, _logger);
    }

    private SovosInvoiceProvider CreateConfiguredProvider()
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

    // ════ 1. CreateEFatura — e-Fatura gonderim ════

    [Fact]
    public async Task CreateEFatura_ValidInvoice_ReturnsSendResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026030900001"",
                    ""pdfUrl"": ""https://sovos.example.com/pdf/GIB2026030900001""
                }"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026030900001");
        result.PdfUrl.Should().Contain("GIB2026030900001");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. CreateEArsiv — e-Arsiv gonderim ════

    [Fact]
    public async Task CreateEArsiv_ValidInvoice_ReturnsSendResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026030900002"",
                    ""pdfUrl"": ""https://sovos.example.com/pdf/GIB2026030900002""
                }"));

        // Act
        var result = await provider.CreateEArsivAsync(CreateTestInvoice("ARSIV-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026030900002");
    }

    // ════ 3. CreateEIrsaliye — e-Irsaliye gonderim ════

    [Fact]
    public async Task CreateEIrsaliye_ValidDispatch_ReturnsSendResult()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/dispatches/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""IRS2026030900001"",
                    ""pdfUrl"": null
                }"));

        // Act
        var result = await provider.CreateEIrsaliyeAsync(CreateTestInvoice("IRS-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("IRS2026030900001");
    }

    // ════ 4. CheckStatus — Accepted invoice ════

    [Fact]
    public async Task CheckStatus_AcceptedInvoice_ReturnsAccepted()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Accepted"",
                    ""acceptedAt"": ""2026-03-09T14:30:00Z"",
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

    // ════ 5. CheckStatus — Pending invoice ════

    [Fact]
    public async Task CheckStatus_PendingInvoice_ReturnsPending()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/invoices/{TestGibInvoiceId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"": ""Pending""}"));

        // Act
        var result = await provider.CheckStatusAsync(TestGibInvoiceId);

        // Assert
        result.Status.Should().Be("Pending");
        result.AcceptedAt.Should().BeNull();
    }

    // ════ 6. GetPdf — PDF indirme ════

    [Fact]
    public async Task GetPdf_ExistingInvoice_ReturnsPdfBytes()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF- header

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/invoices/{TestGibInvoiceId}/pdf").UsingGet())
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

    // ════ 7. IsEInvoiceTaxpayer — Registered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_RegisteredVKN_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "1234567890";

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": true, ""title"": ""Test Musteri A.S.""}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 8. IsEInvoiceTaxpayer — Unregistered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_UnregisteredVKN_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "9999999999";

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/taxpayers/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": false}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 9. CancelInvoice — Valid cancel ════

    [Fact]
    public async Task CancelInvoice_ValidId_ReturnsSuccess()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/invoices/{TestGibInvoiceId}/cancel").UsingPost())
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

    // ════ 10. EnsureConfigured guard ════

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        // Arrange — do NOT call Configure()
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.CreateEFaturaAsync(CreateTestInvoice());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    // ════ 11. Unauthorized — 401 error ════

    [Fact]
    public async Task GetPdf_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"/api/invoices/{TestGibInvoiceId}/pdf").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"": ""Unauthorized""}"));

        // Act & Assert — GetPdf uses EnsureSuccessStatusCode() → throws
        var act = () => provider.GetPdfAsync(TestGibInvoiceId);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ════ 12. Server error — 500 returns failure ════

    [Fact]
    public async Task CreateEFatura_ServerError_ReturnsFalse()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
