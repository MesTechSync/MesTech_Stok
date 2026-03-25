using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// ParasutERPAdapter tests — TestConnection, SyncInvoices, SyncExpenses, SyncCounterparties, GetBalance.
/// Uses mocked HttpClient + ParasutTokenService.
/// </summary>
[Trait("Category", "Unit")]
public class ParasutERPAdapterTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ParasutTokenService _tokenService;
    private readonly ParasutERPAdapter _sut;

    public ParasutERPAdapterTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.parasut.com")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:Parasut:ClientId"] = "test-client",
                ["ERP:Parasut:ClientSecret"] = "test-secret",
                ["ERP:Parasut:CompanyId"] = "12345"
            })
            .Build();

        var cache = new MemoryCache(new MemoryCacheOptions());
        // Pre-populate token cache so token service doesn't call API
        cache.Set("Parasut:AccessToken", "test-token", TimeSpan.FromMinutes(50));

        _tokenService = new ParasutTokenService(
            _httpClient,
            cache,
            config,
            new Mock<ILogger<ParasutTokenService>>().Object);

        _sut = new ParasutERPAdapter(
            _httpClient,
            _tokenService,
            new Mock<MesTech.Domain.Interfaces.IOrderRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IInvoiceRepository>().Object,
            new Mock<ILogger<ParasutERPAdapter>>().Object);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content = "{}")
    {
        _httpHandlerMock
            .Protected()
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

    [Fact]
    public void ERPName_ReturnsParasut()
    {
        _sut.ERPName.Should().Be("Parasut");
    }

    [Fact]
    public async Task TestConnection_Success_ReturnsTrue()
    {
        SetupHttpResponse(HttpStatusCode.OK, """{"data":[]}""");

        var result = await _sut.TestConnectionAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_Failure_ReturnsFalse()
    {
        SetupHttpResponse(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");

        var result = await _sut.TestConnectionAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnection_Exception_ReturnsFalse()
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _sut.TestConnectionAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SyncInvoices_ValidInvoice_PostsToSalesInvoices()
    {
        SetupHttpResponse(HttpStatusCode.Created, """{"data":{"id":"1"}}""");

        var invoice = CreateMockInvoice();
        var invoices = new List<InvoiceEntity> { invoice };

        // Should not throw
        await _sut.SyncInvoicesAsync(invoices);

        _httpHandlerMock
            .Protected()
            .Verify("SendAsync", Times.AtLeast(1),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SyncInvoices_NullList_ThrowsArgumentNull()
    {
        var act = async () => await _sut.SyncInvoicesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncExpenses_ValidExpense_PostsToPurchaseBills()
    {
        SetupHttpResponse(HttpStatusCode.Created, """{"data":{"id":"2"}}""");

        var expenses = new List<AccountingExpenseDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Office Supplies",
                Amount = 500m,
                Category = "General",
                ExpenseDate = DateTime.UtcNow,
                Source = "Manual"
            }
        };

        await _sut.SyncExpensesAsync(expenses);

        _httpHandlerMock
            .Protected()
            .Verify("SendAsync", Times.AtLeast(1),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SyncExpenses_NullList_ThrowsArgumentNull()
    {
        var act = async () => await _sut.SyncExpensesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncCounterparties_ValidContact_PostsToContacts()
    {
        SetupHttpResponse(HttpStatusCode.Created, """{"data":{"id":"3"}}""");

        var parties = new List<CounterpartyDto>
        {
            new()
            {
                Name = "Test Firma",
                VKN = "1234567890"
            }
        };

        await _sut.SyncCounterpartiesAsync(parties);

        _httpHandlerMock
            .Protected()
            .Verify("SendAsync", Times.AtLeast(1),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SyncCounterparties_NullList_ThrowsArgumentNull()
    {
        var act = async () => await _sut.SyncCounterpartiesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetBalance_ValidCode_ReturnsDecimal()
    {
        var responseJson = """
        {
            "data": [
                {
                    "id": "1",
                    "type": "accounts",
                    "attributes": {
                        "balance": "15250.75"
                    }
                }
            ]
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var balance = await _sut.GetBalanceAsync("100-01");
        balance.Should().Be(15250.75m);
    }

    [Fact]
    public async Task GetBalance_NoAccount_ReturnsZero()
    {
        SetupHttpResponse(HttpStatusCode.OK, """{"data":[]}""");

        var balance = await _sut.GetBalanceAsync("999-99");
        balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalance_ServerError_ReturnsZero()
    {
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        var balance = await _sut.GetBalanceAsync("100-01");
        balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalance_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var act = async () => await _sut.GetBalanceAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBalance_Exception_ReturnsZero()
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var balance = await _sut.GetBalanceAsync("100-01");
        balance.Should().Be(0m);
    }

    [Fact]
    public async Task SyncInvoices_ApiFailure_DoesNotThrow()
    {
        SetupHttpResponse(HttpStatusCode.BadRequest, """{"errors":[{"detail":"validation failed"}]}""");

        var invoice = CreateMockInvoice();
        var invoices = new List<InvoiceEntity> { invoice };

        // Should not throw — logs warning and continues
        var act = async () => await _sut.SyncInvoicesAsync(invoices);
        await act.Should().NotThrowAsync();
    }

    private static InvoiceEntity CreateMockInvoice()
    {
        var invoice = new InvoiceEntity
        {
            TenantId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            CustomerName = "Test Customer",
            InvoiceDate = DateTime.UtcNow
        };
        invoice.SetFinancials(1000m, 180m, 1180m);
        return invoice;
    }
}
