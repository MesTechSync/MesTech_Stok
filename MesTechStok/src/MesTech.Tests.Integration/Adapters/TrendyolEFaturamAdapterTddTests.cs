using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// TrendyolEFaturamAdapter contract tests — IInvoiceAdapter evrensel 6 metod.
/// WireMock: TrendyolEFaturamProvider URL pattern /suppliers/123456/e-invoices/...
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "TrendyolEFaturam")]
[Trait("Phase", "Dalga5")]
public class TrendyolEFaturamAdapterTddTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;

    private const string TestApiKey = "test-adapter-api-key";
    private const long TestSupplierId = 123456;
    private const string SupplierPath = "/suppliers/123456/e-invoices";
    private const string TestGibId = "GIB2026031099999";

    public TrendyolEFaturamAdapterTddTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    public void Dispose() => _fixture.Reset();

    private IInvoiceAdapter CreateAdapter()
    {
        var httpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var providerLogger = new Mock<ILogger<TrendyolEFaturamProvider>>().Object;
        var provider = new TrendyolEFaturamProvider(httpClient, providerLogger);
        provider.Configure(TestApiKey, TestSupplierId, _fixture.BaseUrl);

        var adapterLogger = new Mock<ILogger<TrendyolEFaturamAdapter>>().Object;
        return new TrendyolEFaturamAdapter(provider, adapterLogger);
    }

    private static InvoiceCreateRequest BuildRequest(
        string? taxNumber = "1234567890",
        InvoiceType type = InvoiceType.EFatura)
    {
        return new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            PlatformOrderId = "ORD-TEST-001",
            Type = type,
            Customer = new InvoiceCustomerInfo(
                "Test Firma A.S.", taxNumber, "Kadikoy", "Istanbul, Turkiye", null, null),
            TotalAmount = 1180m,
            DefaultKdv = KdvRate.Yuzde20,
            Lines = new List<InvoiceCreateLine>
            {
                new("Test Urun", "SKU-001", 1, 1000m, 0.20m, null)
            }
        };
    }

    // ════ 1. CreateInvoice — e-Fatura success ════

    [Fact]
    public async Task CreateEFatura_ValidInvoice_ReturnsSuccessWithGibId()
    {
        // Arrange
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""gibInvoiceId"": ""GIB2026031000010"", ""pdfUrl"": ""https://efaturam.trendyol.com/pdf/GIB2026031000010""}"));

        // Act
        var result = await adapter.CreateInvoiceAsync(BuildRequest(type: InvoiceType.EFatura));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000010");
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 2. CreateInvoice — API returns error ════

    [Fact]
    public async Task CreateEFatura_ApiReturnsError_ReturnsFail()
    {
        // Arrange
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(@"{""error"": ""Invalid tax number format""}"));

        // Act
        var result = await adapter.CreateInvoiceAsync(BuildRequest(taxNumber: "000", type: InvoiceType.EFatura));

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.GibInvoiceId.Should().BeNull();
    }

    // ════ 3. CreateInvoice — e-Arsiv success ════

    [Fact]
    public async Task CreateEArsiv_ValidInvoice_ReturnsSuccess()
    {
        // Arrange
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/earsiv").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""gibInvoiceId"": ""GIB2026031000011"", ""pdfUrl"": null}"));

        // Act
        var result = await adapter.CreateInvoiceAsync(BuildRequest(taxNumber: null, type: InvoiceType.EArsiv));

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().Be("GIB2026031000011");
    }

    // ════ 4. GetInvoiceStatus — maps status string to enum ════

    [Fact]
    public async Task CheckStatus_ExistingInvoice_ReturnsStatusResult()
    {
        // Arrange
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{TestGibId}/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""status"": ""Accepted"",
                    ""acceptedAt"": ""2026-03-10T12:00:00Z"",
                    ""errorMessage"": null
                }}"));

        // Act
        var result = await adapter.GetInvoiceStatusAsync(TestGibId);

        // Assert
        result.GibInvoiceId.Should().Be(TestGibId);
        result.Status.Should().Be(InvoiceStatus.Accepted);
        result.Description.Should().Be("Accepted");
        result.ResponseDate.Should().NotBeNull();
    }

    // ════ 5. GetInvoicePdf — returns PDF bytes ════

    [Fact]
    public async Task GetPdf_ExistingInvoice_ReturnsNonEmptyByteArray()
    {
        // Arrange
        var adapter = CreateAdapter();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{TestGibId}/pdf").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(fakePdf));

        // Act
        var pdf = await adapter.GetInvoicePdfAsync(TestGibId);

        // Assert
        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be(0x25); // '%'
    }

    // ════ 6. IsEFaturaMukellef — registered VKN returns true ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_RegisteredTaxNumber_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateAdapter();
        var vkn = "1234567890";

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/taxpayer/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": true, ""title"": ""Test Firma A.S.""}"));

        // Act
        var result = await adapter.IsEFaturaMukellefAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 7. IsEFaturaMukellef — unregistered VKN returns false ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_UnregisteredTaxNumber_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter();
        var vkn = "9999999999";

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/taxpayer/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": false}"));

        // Act
        var result = await adapter.IsEFaturaMukellefAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 8. CancelInvoice — success ════

    [Fact]
    public async Task CancelInvoice_ValidInvoice_ReturnsSuccess()
    {
        // Arrange
        var adapter = CreateAdapter();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/{TestGibId}/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await adapter.CancelInvoiceAsync(TestGibId, "Musteri talebi");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }
}
