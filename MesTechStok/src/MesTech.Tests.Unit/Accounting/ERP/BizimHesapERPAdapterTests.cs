using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// BizimHesapERPAdapter tests — TestConnection, SyncInvoices, SyncExpenses, SyncCounterparties, GetBalance.
/// Uses mocked HttpClient via BizimHesapApiClient.
/// </summary>
[Trait("Category", "Unit")]
public class BizimHesapERPAdapterTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly BizimHesapApiClient _apiClient;
    private readonly BizimHesapERPAdapter _sut;

    public BizimHesapERPAdapterTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object)
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

        _apiClient = new BizimHesapApiClient(
            _httpClient,
            config,
            new Mock<ILogger<BizimHesapApiClient>>().Object);

        _sut = new BizimHesapERPAdapter(
            _apiClient,
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

    private void SetupSequentialHttpResponses(params (HttpStatusCode status, string content)[] responses)
    {
        var setup = _httpHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var (status, content) in responses)
        {
            setup.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content)
            });
        }
    }

    // ── ERPName ──

    [Fact]
    public void ERPName_ReturnsBizimHesap()
    {
        _sut.ERPName.Should().Be("BizimHesap");
    }

    // ── TestConnection ──

    [Fact]
    public async Task TestConnection_Success_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"company\":\"test\"}");

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_Failure_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, "{\"error\":\"invalid api key\"}");

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnection_NetworkError_ReturnsFalse()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("DNS failure"));

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    // ── SyncInvoices ──

    [Fact]
    public async Task SyncInvoices_ValidData_PostsToInvoices()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":\"inv-1\"}");

        var bhInv = new InvoiceEntity
        {
            InvoiceNumber = "BH-INV-001",
            CustomerName = "Musteri A",
            Currency = "TRY"
        };
        bhInv.SetFinancials(2000m, 360m, 2360m);
        var invoices = new List<InvoiceEntity> { bhInv };

        // Act
        var act = () => _sut.SyncInvoicesAsync(invoices);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncInvoices_NullList_ThrowsArgumentNull()
    {
        var act = () => _sut.SyncInvoicesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncInvoices_MultipleInvoices_ProcessesAll()
    {
        // Arrange
        SetupSequentialHttpResponses(
            (HttpStatusCode.OK, "{\"id\":1}"),
            (HttpStatusCode.OK, "{\"id\":2}"),
            (HttpStatusCode.OK, "{\"id\":3}"));

        var invoices = Enumerable.Range(1, 3)
            .Select(i =>
            {
                var inv = new InvoiceEntity { InvoiceNumber = $"BH-INV-{i:D3}" };
                inv.SetFinancials(i * 100m, 0m, i * 100m);
                return inv;
            }).ToList();

        // Act
        var act = () => _sut.SyncInvoicesAsync(invoices);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── SyncExpenses ──

    [Fact]
    public async Task SyncExpenses_ValidData_PostsToExpenses()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":\"exp-1\"}");

        var expenses = new List<AccountingExpenseDto>
        {
            new AccountingExpenseDto
            {
                Title = "Kargo Gideri",
                Amount = 150m,
                Category = "Lojistik",
                ExpenseDate = DateTime.UtcNow
            }
        };

        // Act
        var act = () => _sut.SyncExpensesAsync(expenses);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncExpenses_NullList_ThrowsArgumentNull()
    {
        var act = () => _sut.SyncExpensesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncExpenses_ServerError_ContinuesProcessing()
    {
        // Arrange — first fails, second succeeds
        SetupSequentialHttpResponses(
            (HttpStatusCode.InternalServerError, "error"),
            (HttpStatusCode.OK, "{\"id\":2}"));

        var expenses = new List<AccountingExpenseDto>
        {
            new() { Title = "FAIL", Amount = 100m },
            new() { Title = "OK", Amount = 200m }
        };

        // Act
        var act = () => _sut.SyncExpensesAsync(expenses);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── SyncCounterparties ──

    [Fact]
    public async Task SyncCounterparties_UpsertByVKN()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":\"cust-1\"}");

        var parties = new List<CounterpartyDto>
        {
            new CounterpartyDto
            {
                Name = "XYZ Ticaret",
                VKN = "9876543210",
                CounterpartyType = "Customer",
                Email = "info@xyz.com"
            }
        };

        // Act
        var act = () => _sut.SyncCounterpartiesAsync(parties);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncCounterparties_NullList_ThrowsArgumentNull()
    {
        var act = () => _sut.SyncCounterpartiesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetBalance ──

    [Fact]
    public async Task GetBalance_ValidCode_ReturnsDecimal()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"code\":\"100.01\",\"balance\":\"8750.25\"}");

        // Act
        var result = await _sut.GetBalanceAsync("100.01");

        // Assert
        result.Should().Be(8750.25m);
    }

    [Fact]
    public async Task GetBalance_NotFound_ReturnsZero()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, "{\"error\":\"account not found\"}");

        // Act
        var result = await _sut.GetBalanceAsync("999.99");

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalance_NullCode_ThrowsArgumentException()
    {
        var act = () => _sut.GetBalanceAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBalance_WhitespaceCode_ThrowsArgumentException()
    {
        var act = () => _sut.GetBalanceAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── API Key ──

    [Fact]
    public async Task ApiKey_SentInHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"company\":\"test\"}")
            });

        // Act
        await _sut.TestConnectionAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "X-BizimHesap-ApiKey");
        capturedRequest.Headers.GetValues("X-BizimHesap-ApiKey").First().Should().Be("test-api-key-bh");
    }

    [Fact]
    public async Task GetBalance_NetworkError_ReturnsZero()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        // Act
        var result = await _sut.GetBalanceAsync("100.01");

        // Assert
        result.Should().Be(0m);
    }
}
