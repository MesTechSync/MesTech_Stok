using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Infrastructure.Integration.ERP.ERPNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// ERPNextRestAdapter unit tests — Frappe REST API mock-based.
/// TestConnection, SyncInvoices, SyncCounterparties, GetBalance, error handling.
/// </summary>
[Trait("Category", "Unit")]
public class ERPNextRestAdapterTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly ERPNextRestAdapter _sut;

    private static readonly ERPNextOptions TestOptions = new()
    {
        BaseUrl = "https://erp.test.mestech.app",
        ApiKey = "test-api-key",
        ApiSecret = "test-api-secret",
        Company = "TestCo",
        DefaultWarehouse = "Stores - TC",
        Enabled = true
    };

    public ERPNextRestAdapterTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpHandlerMock.Object);

        _sut = new ERPNextRestAdapter(
            httpClient,
            new Mock<ILogger<ERPNextRestAdapter>>().Object,
            Options.Create(TestOptions));
    }

    [Fact]
    public void ERPName_Returns_ERPNext()
    {
        _sut.ERPName.Should().Be("ERPNext");
    }

    [Fact]
    public async Task TestConnectionAsync_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, """{"message":"testuser@mestech.app"}""");

        var result = await _sut.TestConnectionAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_Unauthorized_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Unauthorized, """{"exc_type":"AuthenticationError"}""");

        var result = await _sut.TestConnectionAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_NetworkError_ReturnsFalse()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unreachable"));

        var result = await _sut.TestConnectionAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SyncInvoicesAsync_CreatesDoc_LogsSuccess()
    {
        SetupResponse(HttpStatusCode.OK, """{"data":{"name":"SINV-00001"}}""");

        var invoice = CreateTestInvoice();

        await _sut.Invoking(s => s.SyncInvoicesAsync(new[] { invoice }, CancellationToken.None))
            .Should().NotThrowAsync();

        VerifyHttpCalled(HttpMethod.Post, "api/resource/Sales Invoice");
    }

    [Fact]
    public async Task SyncInvoicesAsync_ServerError_DoesNotThrow()
    {
        SetupResponse(HttpStatusCode.InternalServerError, """{"exc":"Server Error"}""");

        var invoice = CreateTestInvoice();

        // Should catch and log, not throw — adapter contract
        await _sut.Invoking(s => s.SyncInvoicesAsync(new[] { invoice }, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncCounterpartiesAsync_Customer_PostsCustomerDocType()
    {
        SetupResponse(HttpStatusCode.OK, """{"data":{"name":"CUST-00001"}}""");

        var party = new CounterpartyDto
        {
            Name = "Test Customer",
            TaxId = "1234567890",
            IsSupplier = false,
            IsCompany = true
        };

        await _sut.SyncCounterpartiesAsync(new[] { party }, CancellationToken.None);

        VerifyHttpCalled(HttpMethod.Post, "api/resource/Customer");
    }

    [Fact]
    public async Task SyncCounterpartiesAsync_Supplier_PostsSupplierDocType()
    {
        SetupResponse(HttpStatusCode.OK, """{"data":{"name":"SUP-00001"}}""");

        var party = new CounterpartyDto
        {
            Name = "Test Supplier",
            TaxId = "9876543210",
            IsSupplier = true,
            IsCompany = true
        };

        await _sut.SyncCounterpartiesAsync(new[] { party }, CancellationToken.None);

        VerifyHttpCalled(HttpMethod.Post, "api/resource/Supplier");
    }

    [Fact]
    public async Task GetBalanceAsync_ValidResponse_ReturnsDecimal()
    {
        SetupResponse(HttpStatusCode.OK, """{"message":12345.67}""");

        var balance = await _sut.GetBalanceAsync("1100 - Accounts Receivable - TC");

        balance.Should().Be(12345.67m);
    }

    [Fact]
    public async Task GetBalanceAsync_MissingMessage_ReturnsZero()
    {
        SetupResponse(HttpStatusCode.OK, """{"other":"field"}""");

        var balance = await _sut.GetBalanceAsync("1100");

        balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_NetworkError_ReturnsZero()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var balance = await _sut.GetBalanceAsync("1100");

        balance.Should().Be(0m);
    }

    [Fact]
    public void Constructor_NotConfigured_ThrowsOnTestConnection()
    {
        var disabledOptions = new ERPNextOptions { Enabled = false };
        var adapter = new ERPNextRestAdapter(
            new HttpClient(),
            new Mock<ILogger<ERPNextRestAdapter>>().Object,
            Options.Create(disabledOptions));

        adapter.Invoking(s => s.TestConnectionAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task SyncExpensesAsync_CreatesDoc_PostsPurchaseInvoice()
    {
        SetupResponse(HttpStatusCode.OK, """{"data":{"name":"PINV-00001"}}""");

        var expense = new AccountingExpenseDto
        {
            Id = Guid.NewGuid(),
            SupplierName = "Test Supplier",
            Date = DateTime.UtcNow,
            Amount = 500m,
            Description = "Kargo masrafı",
            Category = "Shipping",
            GlAccountCode = "5100"
        };

        await _sut.SyncExpensesAsync(new[] { expense }, CancellationToken.None);

        VerifyHttpCalled(HttpMethod.Post, "api/resource/Purchase Invoice");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupResponse(HttpStatusCode statusCode, string body)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private void VerifyHttpCalled(HttpMethod method, string urlContains)
    {
        _httpHandlerMock.Protected()
            .Verify("SendAsync", Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == method &&
                    r.RequestUri != null &&
                    r.RequestUri.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>());
    }

    private static MesTech.Domain.Entities.Invoice CreateTestInvoice()
    {
        // Invoice.Create factory — minimal valid instance for adapter test
        return MesTech.Domain.Entities.Invoice.Create(
            tenantId: Guid.NewGuid(),
            orderId: Guid.NewGuid(),
            invoiceNumber: "INV-TEST-001",
            customer: "Test Müşteri A.Ş.",
            grandTotal: 1180.00m,
            taxAmount: 180.00m);
    }
}
