using System.Net;
using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// BizimHesapERPAdapter IErpInvoiceCapable contract tests.
/// Tests: CreateInvoice, GetInvoice, GetInvoices, CancelInvoice.
/// </summary>
[Trait("Category", "Unit")]
public class BizimHesapInvoiceCapableTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly IErpInvoiceCapable _sut;

    public BizimHesapInvoiceCapableTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.bizimhesap.com")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:BizimHesap:BaseUrl"] = "https://api.bizimhesap.com/v1",
                ["ERP:BizimHesap:ApiKey"] = "test-api-key-bh"
            })
            .Build();

        var apiClient = new BizimHesapApiClient(
            httpClient,
            config,
            new Mock<ILogger<BizimHesapApiClient>>().Object);

        _sut = new BizimHesapERPAdapter(
            apiClient,
            new Mock<MesTech.Domain.Interfaces.IOrderRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IInvoiceRepository>().Object,
            new Mock<ILogger<BizimHesapERPAdapter>>().Object);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content)
            });
    }

    private static ErpInvoiceRequest MakeInvoiceRequest() => new(
        CustomerCode: "CUST-001",
        CustomerName: "Test Musteri",
        TaxId: "1234567890",
        Lines: new List<ErpInvoiceLineRequest>
        {
            new("PROD-001", "Urun A", 2, 100m, 18, null),
            new("PROD-002", "Urun B", 1, 50m, 8, 5m)
        },
        SubTotal: 250m,
        TaxTotal: 40m,
        GrandTotal: 290m,
        Currency: "TRY",
        Notes: "Test fatura"
    );

    // ── CreateInvoice ──

    [Fact]
    public async Task CreateInvoice_Success_ReturnsOkResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""id"":""bh-inv-001"",""invoiceNumber"":""BH-2026-001"",""invoiceDate"":""2026-03-23T00:00:00Z"",""grandTotal"":""290.00"",""pdfUrl"":""https://bh.com/pdf/001""}");

        // Act
        var result = await _sut.CreateInvoiceAsync(MakeInvoiceRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.InvoiceNumber.Should().Be("BH-2026-001");
        result.ErpRef.Should().Be("bh-inv-001");
        result.GrandTotal.Should().Be(290m);
        result.PdfUrl.Should().Be("https://bh.com/pdf/001");
    }

    [Fact]
    public async Task CreateInvoice_ServerError_ReturnsFailedResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, @"{""error"":""Internal error""}");

        // Act
        var result = await _sut.CreateInvoiceAsync(MakeInvoiceRequest());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateInvoice_NullRequest_ThrowsArgumentNull()
    {
        var act = () => _sut.CreateInvoiceAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetInvoice ──

    [Fact]
    public async Task GetInvoice_Found_ReturnsResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""id"":""bh-inv-002"",""invoiceNumber"":""BH-2026-002"",""invoiceDate"":""2026-03-20T00:00:00Z"",""grandTotal"":""500.00""}");

        // Act
        var result = await _sut.GetInvoiceAsync("BH-2026-002");

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.InvoiceNumber.Should().Be("BH-2026-002");
        result.GrandTotal.Should().Be(500m);
    }

    [Fact]
    public async Task GetInvoice_NotFound_ReturnsNull()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""not found""}");

        // Act
        var result = await _sut.GetInvoiceAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoice_NullInput_ThrowsArgumentException()
    {
        var act = () => _sut.GetInvoiceAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetInvoices ──

    [Fact]
    public async Task GetInvoices_DateRange_ReturnsList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"[{""id"":""1"",""invoiceNumber"":""INV-A"",""invoiceDate"":""2026-03-10T00:00:00Z"",""grandTotal"":""100.00""},{""id"":""2"",""invoiceNumber"":""INV-B"",""invoiceDate"":""2026-03-15T00:00:00Z"",""grandTotal"":""200.50""}]");

        // Act
        var result = await _sut.GetInvoicesAsync(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        // Assert
        result.Should().HaveCount(2);
        result[0].InvoiceNumber.Should().Be("INV-A");
        result[1].GrandTotal.Should().Be(200.50m);
    }

    [Fact]
    public async Task GetInvoices_ServerError_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "error");

        // Act
        var result = await _sut.GetInvoicesAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().BeEmpty();
    }

    // ── CancelInvoice ──

    [Fact]
    public async Task CancelInvoice_Success_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, @"{""status"":""cancelled""}");

        // Act
        var result = await _sut.CancelInvoiceAsync("BH-2026-001", "Test cancellation");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelInvoice_NotFound_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""not found""}");

        // Act
        var result = await _sut.CancelInvoiceAsync("NONEXISTENT", "reason");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelInvoice_NullInput_ThrowsArgumentException()
    {
        var act = () => _sut.CancelInvoiceAsync(null!, "reason");
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
