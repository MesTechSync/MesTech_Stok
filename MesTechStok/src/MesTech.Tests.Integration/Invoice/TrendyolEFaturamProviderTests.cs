using System.Net;
using System.Net.Http;
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

namespace MesTech.Tests.Integration.Invoice;

/// <summary>
/// Trendyol e-Faturam provider contract tests — REST JSON API + Bearer auth.
/// 15 WireMock tests covering IInvoiceProvider + IBulkInvoiceCapable + IKontorCapable + IInvoiceTemplateCapable.
/// URL pattern: /suppliers/{supplierId}/e-invoices/...
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "TrendyolEFaturam")]
public class TrendyolEFaturamProviderTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly ILogger<TrendyolEFaturamProvider> _logger;

    private const string TestApiKey = "test-trendyol-efaturam-api-key-99999";
    private const long TestSupplierId = 123456;
    private const string SupplierPath = "/suppliers/123456/e-invoices";
    private const string TestGibInvoiceId = "GIB2026031000001";

    public TrendyolEFaturamProviderTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _logger = new Mock<ILogger<TrendyolEFaturamProvider>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    // ── Factory ──────────────────────────────────────────────────────────

    private TrendyolEFaturamProvider CreateProvider()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new TrendyolEFaturamProvider(httpClient, _logger);
    }

    private TrendyolEFaturamProvider CreateConfiguredProvider()
    {
        var provider = CreateProvider();
        provider.Configure(TestApiKey, TestSupplierId, _fixture.BaseUrl);
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

    // ════ 1. CreateEFatura — e-Fatura gonderim ════

    [Fact]
    public async Task CreateEFatura_Success_ReturnsGibInvoiceId()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/efatura").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026031000001"",
                    ""pdfUrl"": ""https://efaturam.trendyol.com/pdf/GIB2026031000001""
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
            .Given(Request.Create().WithPath($"{SupplierPath}/earsiv").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""gibInvoiceId"": ""GIB2026031000002"",
                    ""pdfUrl"": ""https://efaturam.trendyol.com/pdf/GIB2026031000002""
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
            .Given(Request.Create().WithPath($"{SupplierPath}/eirsaliye").UsingPost())
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

    // ════ 4. CheckStatus — Accepted invoice ════

    [Fact]
    public async Task CheckStatus_ReturnsStatus()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/{TestGibInvoiceId}/status").UsingGet())
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

    // ════ 5. GetPdf — PDF indirme ════

    [Fact]
    public async Task GetPdf_ReturnsPdfBytes()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var fakePdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF- header

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/{TestGibInvoiceId}/pdf").UsingGet())
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

    // ════ 6. IsEInvoiceTaxpayer — Registered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_ReturnsTrueForRegistered()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "1234567890";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/taxpayer/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": true, ""title"": ""Test Musteri A.S.""}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 7. IsEInvoiceTaxpayer — Unregistered VKN ════

    [Fact]
    public async Task IsEInvoiceTaxpayer_ReturnsFalseForUnregistered()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var vkn = "9999999999";

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/taxpayer/{vkn}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isRegistered"": false}"));

        // Act
        var result = await provider.IsEInvoiceTaxpayerAsync(vkn);

        // Assert
        result.Should().BeFalse();
    }

    // ════ 8. CancelInvoice — Valid cancel ════

    [Fact]
    public async Task CancelInvoice_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/{TestGibInvoiceId}/cancel").UsingPost())
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

    // ════ 9. CancelInvoice — Server error returns fail ════

    [Fact]
    public async Task CancelInvoice_ServerError_ReturnsFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"{SupplierPath}/{TestGibInvoiceId}/cancel").UsingPost())
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

    // ════ 10. CreateBulkInvoice — All success ════

    [Fact]
    public async Task CreateBulkInvoice_Success_ReturnsAllResults()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("BULK-001"), CreateTestInvoiceRequest("BULK-002") };

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/bulk").UsingPost())
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

    // ════ 11. CreateBulkInvoice — Partial failure ════

    [Fact]
    public async Task CreateBulkInvoice_PartialFailure_ReturnsCorrectCounts()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("BULK-003"), CreateTestInvoiceRequest("BULK-004") };

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/bulk").UsingPost())
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

    // ════ 12. GetKontorBalance — Returns balance info ════

    [Fact]
    public async Task GetKontorBalance_ReturnsBalance()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/kontor").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""remaining"": 320,
                    ""total"": 500,
                    ""expiresAt"": ""2026-12-31T23:59:59Z""
                }"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(320);
        result.TotalKontor.Should().Be(500);
        result.ExpiresAt.Should().NotBeNull();
        result.ProviderName.Should().Be("Trendyol e-Faturam");
    }

    // ════ 13. GetKontorBalance — Server error returns default ════

    [Fact]
    public async Task GetKontorBalance_ServerError_ReturnsDefault()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/kontor").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(0);
        result.TotalKontor.Should().Be(0);
        result.ExpiresAt.Should().BeNull();
        result.ProviderName.Should().Be("Trendyol e-Faturam");
    }

    // ════ 14. SetInvoiceTemplate — Returns true ════

    [Fact]
    public async Task SetInvoiceTemplate_Success_ReturnsTrue()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var template = new InvoiceTemplateDto(
            LogoImage: new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            SignatureImage: new byte[] { 0xFF, 0xD8, 0xFF },
            PhoneNumber: "+90 212 555 0000",
            Email: "fatura@mestech.com",
            TicaretSicilNo: "123456",
            ShowKargoBarkodu: true,
            ShowFaturaTutariYaziyla: true,
            DefaultKdv: KdvRate.Yuzde20);

        _fixture.Server
            .Given(Request.Create().WithPath($"{SupplierPath}/template").UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.SetInvoiceTemplateAsync(template);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 15. NotConfigured — throws InvalidOperationException ════

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
