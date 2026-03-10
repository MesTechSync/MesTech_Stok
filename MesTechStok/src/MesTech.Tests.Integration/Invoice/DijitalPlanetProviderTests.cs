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
/// Dijital Planet provider contract tests — pure REST JSON API + Bearer auth.
/// 12 WireMock tests covering IInvoiceProvider (base only, no extra capability interfaces).
/// URL pattern: /api/invoices/...
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "DijitalPlanet")]
public class DijitalPlanetProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<DijitalPlanetProvider> _logger;

    private const string TestBearerToken = "test-dijitalplanet-bearer-token-77777";
    private const string TestGibInvoiceId = "GIB2026031000001";
    private const string ApiBase = "/api";

    public DijitalPlanetProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<DijitalPlanetProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private DijitalPlanetProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new DijitalPlanetProvider(httpClient, _logger);
    }

    private DijitalPlanetProvider CreateConfiguredProvider()
    {
        var provider = CreateProvider();
        provider.Configure(TestBearerToken, _fixture.BaseUrl);
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
    public async Task CreateEFatura_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026031000001"",
                    ""pdfUrl"": ""https://dijitalplanet.com/pdf/GIB2026031000001""
                }"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000001");
        result.PdfUrl.Should().Contain("GIB2026031000001");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. CreateEArsiv — e-Arsiv gonderim ════

    [Fact]
    public async Task CreateEArsiv_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/earsiv").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026031000002"",
                    ""pdfUrl"": ""https://dijitalplanet.com/pdf/GIB2026031000002""
                }"));

        // Act
        var result = await provider.CreateEArsivAsync(CreateTestInvoice("ARSIV-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000002");
    }

    // ════ 3. CreateEIrsaliye — e-Irsaliye gonderim ════

    [Fact]
    public async Task CreateEIrsaliye_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/eirsaliye").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""IRS2026031000001"",
                    ""pdfUrl"": null
                }"));

        // Act
        var result = await provider.CreateEIrsaliyeAsync(CreateTestInvoice("IRS-2026-001"));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("IRS2026031000001");
    }

    // ════ 4. CheckStatus — Returns accepted status ════

    [Fact]
    public async Task CheckStatus_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/status").UsingGet())
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

    // ════ 5. CheckStatus — Server error returns Error status ════

    [Fact]
    public async Task CheckStatus_ServerError_ReturnsErrorStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/status").UsingGet())
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

    // ════ 6. GetPdf — PDF indirme ════

    [Fact]
    public async Task GetPdf_ReturnsPdfBytes()
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

    // ════ 7. IsEInvoiceTaxpayer — Registered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_ReturnsTrueForRegistered()
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
                .WithBody(@"{""isRegistered"": true, ""title"": ""Test Musteri A.S.""}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 8. IsEInvoiceTaxpayer — Unregistered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_ReturnsFalseForUnregistered()
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
                .WithBody(@"{""isRegistered"": false}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 9. CancelInvoice — Valid cancel ════

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
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.CancelInvoiceAsync(TestGibInvoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be(TestGibInvoiceId);
    }

    // ════ 10. CancelInvoice — Server error returns fail ════

    [Fact]
    public async Task CancelInvoice_ServerError_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{ApiBase}/invoices/{TestGibInvoiceId}/cancel").UsingPost())
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

    // ════ 11. CreateEFatura — Auth error returns fail ════

    [Fact]
    public async Task CreateEFatura_AuthError_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{ApiBase}/invoices/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"": ""Unauthorized — invalid bearer token""}"));

        // Act
        var result = await provider.CreateEFaturaAsync(CreateTestInvoice());

        // Assert
        result.Success.Should().BeFalse();
        result.GibInvoiceId.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 12. NotConfigured — throws InvalidOperationException ════

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
