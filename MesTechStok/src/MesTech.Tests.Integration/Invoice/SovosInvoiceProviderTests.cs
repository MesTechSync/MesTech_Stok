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
        var ublBuilder = new Mock<IUblTrXmlBuilder>().Object;
        return new SovosInvoiceProvider(httpClient, _logger, ublBuilder);
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

    // ════ 13. CreateBulkInvoice — All success ════

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
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-001"", ""pdfUrl"": ""https://sovos.example.com/pdf/GIB-BULK-001"", ""errorMessage"": null },
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-002"", ""pdfUrl"": ""https://sovos.example.com/pdf/GIB-BULK-002"", ""errorMessage"": null }
                    ]
                }"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Results[0].Success.Should().BeTrue();
        result.Results[0].GibInvoiceId.Should().Be("GIB-BULK-001");
        result.Results[1].Success.Should().BeTrue();
        result.Results[1].GibInvoiceId.Should().Be("GIB-BULK-002");
    }

    // ════ 14. CreateBulkInvoice — Partial failure ════

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
                        { ""success"": true, ""gibInvoiceId"": ""GIB-BULK-003"", ""pdfUrl"": null, ""errorMessage"": null },
                        { ""success"": false, ""gibInvoiceId"": null, ""pdfUrl"": null, ""errorMessage"": ""Invalid tax number"" }
                    ]
                }"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Results[0].Success.Should().BeTrue();
        result.Results[1].Success.Should().BeFalse();
        result.Results[1].ErrorMessage.Should().Be("Invalid tax number");
    }

    // ════ 15. CreateBulkInvoice — HTTP error returns all-fail ════

    [Fact]
    public async Task CreateBulkInvoice_HttpError_ReturnsAllFail()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var requests = new[] { CreateTestInvoiceRequest("BULK-005"), CreateTestInvoiceRequest("BULK-006") };

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/outgoing/bulk").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.CreateBulkInvoiceAsync(requests);

        // Assert
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.Results.Should().AllSatisfy(r => r.Success.Should().BeFalse());
    }

    // ════ 16. GetIncomingInvoices — Returns list ════

    [Fact]
    public async Task GetIncomingInvoices_ReturnsInvoiceList()
    {
        // Arrange
        var provider = CreateConfiguredProvider();
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 9);

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/api/invoices/incoming")
                .WithParam("from", "2026-03-01")
                .WithParam("to", "2026-03-09")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""invoices"": [
                        {
                            ""gibInvoiceId"": ""GIB-IN-001"",
                            ""senderName"": ""Tedarikci A.S."",
                            ""senderTaxNumber"": ""9876543210"",
                            ""amount"": 5000.00,
                            ""invoiceDate"": ""2026-03-05T00:00:00Z"",
                            ""status"": ""Pending""
                        }
                    ]
                }"));

        // Act
        var result = await provider.GetIncomingInvoicesAsync(from, to);

        // Assert
        result.Should().HaveCount(1);
        result[0].GibInvoiceId.Should().Be("GIB-IN-001");
        result[0].SenderName.Should().Be("Tedarikci A.S.");
        result[0].SenderTaxNumber.Should().Be("9876543210");
        result[0].GrandTotal.Should().Be(5000.00m);
        result[0].Status.Should().Be(InvoiceStatus.Draft); // "Pending" not in enum, defaults to Draft
    }

    // ════ 17. GetIncomingInvoices — Server error returns empty ════

    [Fact]
    public async Task GetIncomingInvoices_ServerError_ReturnsEmptyList()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/invoices/incoming").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.GetIncomingInvoicesAsync(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 9));

        // Assert
        result.Should().BeEmpty();
    }

    // ════ 18. AcceptInvoice — Returns true ════

    [Fact]
    public async Task AcceptInvoice_ReturnsTrue()
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

    // ════ 19. RejectInvoice — Returns true ════

    [Fact]
    public async Task RejectInvoice_ReturnsTrue()
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

    // ════ 20. GetKontorBalance — Returns info ════

    [Fact]
    public async Task GetKontorBalance_ReturnsInfo()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/account/kontor").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""remaining"": 450,
                    ""total"": 500,
                    ""lastChecked"": ""2026-03-09T12:00:00Z""
                }"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(450);
        result.TotalKontor.Should().Be(500);
        result.ExpiresAt.Should().NotBeNull();
    }

    // ════ 21. GetKontorBalance — Server error returns zero ════

    [Fact]
    public async Task GetKontorBalance_ServerError_ReturnsZero()
    {
        // Arrange
        var provider = CreateConfiguredProvider();

        _fixture.Server
            .Given(Request.Create().WithPath("/api/account/kontor").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var result = await provider.GetKontorBalanceAsync();

        // Assert
        result.RemainingKontor.Should().Be(0);
        result.TotalKontor.Should().Be(0);
        result.ExpiresAt.Should().BeNull();
    }

    // ════ 22. SetInvoiceTemplate — Returns true ════

    [Fact]
    public async Task SetInvoiceTemplate_ReturnsTrue()
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
            .Given(Request.Create().WithPath("/api/invoices/template").UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await provider.SetInvoiceTemplateAsync(template);

        // Assert
        result.Should().BeTrue();
    }
}
