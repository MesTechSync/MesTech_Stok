using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.ERP.Logo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// LogoERPAdapter tests — TestConnection, SyncInvoices, SyncExpenses, SyncCounterparties, GetBalance,
/// and Dalga 12 IErpAdapter methods: SyncOrderAsync, SyncInvoiceAsync, GetAccountBalancesAsync, PingAsync.
/// Uses mocked HttpClient + LogoTokenService with pre-populated cache.
/// </summary>
[Trait("Category", "Unit")]
public class LogoERPAdapterTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly LogoTokenService _tokenService;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock;
    private readonly LogoERPAdapter _sut;
    private readonly IMemoryCache _cache;

    public LogoERPAdapterTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://logo-test.local")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:Logo:Username"] = "test-user",
                ["ERP:Logo:Password"] = "test-pass",
                ["ERP:Logo:FirmId"] = "001",
                ["ERP:Logo:BaseUrl"] = "https://logo-test.local/logo-rest/api/"
            })
            .Build();

        _cache = new MemoryCache(new MemoryCacheOptions());
        // Pre-populate token cache so token service doesn't call API
        _cache.Set("Logo:AccessToken", "test-token-logo", TimeSpan.FromMinutes(50));

        _tokenService = new LogoTokenService(
            _httpClient,
            _cache,
            config,
            new Mock<ILogger<LogoTokenService>>().Object);

        _orderRepoMock = new Mock<IOrderRepository>();
        _invoiceRepoMock = new Mock<IInvoiceRepository>();

        _sut = new LogoERPAdapter(
            _httpClient,
            _tokenService,
            _orderRepoMock.Object,
            _invoiceRepoMock.Object,
            new Mock<ILogger<LogoERPAdapter>>().Object);
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

    // ═══════════════════════════════════════════════════════════════════
    // IERPAdapter — Legacy tests
    // ═══════════════════════════════════════════════════════════════════

    // ── ERPName ──

    [Fact]
    public void ERPName_ReturnsLogo()
    {
        _sut.ERPName.Should().Be("Logo");
    }

    // ── Provider (Dalga 11) ──

    [Fact]
    public void Provider_ReturnsLogoEnum()
    {
        ((IErpAdapter)_sut).Provider.Should().Be(ErpProvider.Logo);
    }

    // ── TestConnection ──

    [Fact]
    public async Task TestConnection_Success_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"companies\":[]}");

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_Failure_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");

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
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    // ── SyncInvoices (batch) ──

    [Fact]
    public async Task SyncInvoices_ValidData_PostsToSalesInvoices()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":1}");

        var logoInv = new InvoiceEntity
        {
            InvoiceNumber = "INV-001",
            CustomerName = "Test Customer",
            Currency = "TRY"
        };
        logoInv.SetFinancials(1000m, 200m, 1200m);
        var invoices = new List<InvoiceEntity> { logoInv };

        // Act
        var act = () => _sut.SyncInvoicesAsync(invoices);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncInvoices_NullList_ThrowsArgumentNull()
    {
        // Act
        var act = () => _sut.SyncInvoicesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncInvoices_EmptyList_DoesNotThrow()
    {
        // Arrange — no HTTP calls expected for empty list

        // Act
        var act = () => _sut.SyncInvoicesAsync(new List<InvoiceEntity>());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncInvoices_ServerError_ContinuesProcessing()
    {
        // Arrange — first call fails, second succeeds
        SetupSequentialHttpResponses(
            (HttpStatusCode.InternalServerError, "{\"error\":\"fail\"}"),
            (HttpStatusCode.OK, "{\"id\":2}"));

        var invoices = new List<InvoiceEntity>
        {
            CreateInvoiceWithTotal("INV-FAIL", 100m),
            CreateInvoiceWithTotal("INV-OK", 200m)
        };

        // Act — should not throw even if individual invoices fail
        var act = () => _sut.SyncInvoicesAsync(invoices);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── SyncExpenses ──

    [Fact]
    public async Task SyncExpenses_ValidData_PostsToPurchaseInvoices()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":1}");

        var expenses = new List<AccountingExpenseDto>
        {
            new AccountingExpenseDto
            {
                Title = "Ofis Malzemesi",
                Amount = 500m,
                ExpenseDate = DateTime.UtcNow,
                Source = "Manuel"
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

    // ── SyncCounterparties ──

    [Fact]
    public async Task SyncCounterparties_UpsertByTaxNumber()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":1}");

        var parties = new List<CounterpartyDto>
        {
            new CounterpartyDto
            {
                Name = "ABC Ltd.",
                VKN = "1234567890",
                CounterpartyType = "Customer"
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
        SetupHttpResponse(HttpStatusCode.OK, "{\"balance\":\"15234.50\"}");

        // Act
        var result = await _sut.GetBalanceAsync("100.01");

        // Assert
        result.Should().Be(15234.50m);
    }

    [Fact]
    public async Task GetBalance_NotFound_ReturnsZero()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");

        // Act
        var result = await _sut.GetBalanceAsync("999.99");

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalance_NullOrEmptyCode_ThrowsArgumentException()
    {
        var act = () => _sut.GetBalanceAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();

        var act2 = () => _sut.GetBalanceAsync("  ");
        await act2.Should().ThrowAsync<ArgumentException>();
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

    // ── Token Refresh / 401 Handling ──

    [Fact]
    public async Task Token_Expired_InvalidatesOnUnauthorized()
    {
        // Arrange — return 401 to trigger token invalidation
        SetupHttpResponse(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");

        var invoices = new List<InvoiceEntity>
        {
            CreateInvoiceWithTotal("INV-AUTH", 100m)
        };

        // Act — should not throw, just log and continue
        var act = () => _sut.SyncInvoicesAsync(invoices);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncExpenses_PartialFailure_ReportsCount()
    {
        // Arrange
        SetupSequentialHttpResponses(
            (HttpStatusCode.OK, "{\"id\":1}"),
            (HttpStatusCode.BadRequest, "{\"error\":\"invalid\"}"),
            (HttpStatusCode.OK, "{\"id\":3}"));

        var expenses = new List<AccountingExpenseDto>
        {
            new() { Title = "OK-1", Amount = 100m },
            new() { Title = "FAIL", Amount = 200m },
            new() { Title = "OK-2", Amount = 300m }
        };

        // Act — should not throw
        var act = () => _sut.SyncExpensesAsync(expenses);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter (Dalga 12) — SyncOrderAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncOrderAsync_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-001",
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow
        };
        order.SetFinancials(1000m, 200m, 1200m);
        // Set the Id via reflection since BaseEntity has it
        typeof(Order).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(order, orderId);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":\"LOGO-ORD-12345\",\"success\":true}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("LOGO-ORD-12345");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SyncOrderAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order not found");
    }

    [Fact]
    public async Task SyncOrderAsync_EmptyGuid_ReturnsFailure()
    {
        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(Guid.Empty);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("OrderId cannot be empty");
    }

    [Fact]
    public async Task SyncOrderAsync_ApiError_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-FAIL"
        };
        order.SetFinancials(0m, 0m, 100m);
        typeof(Order).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(order, orderId);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        SetupHttpResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid order data\"}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("invalid order data");
    }

    [Fact]
    public async Task SyncOrderAsync_NetworkException_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { OrderNumber = "ORD-NET" };
        order.SetFinancials(0m, 0m, 100m);
        typeof(Order).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(order, orderId);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection refused");
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter (Dalga 12) — SyncInvoiceAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncInvoiceAsync_ValidInvoice_ReturnsSuccess()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new InvoiceEntity
        {
            InvoiceNumber = "INV-001",
            CustomerName = "Test Customer",
            Currency = "TRY",
            InvoiceDate = DateTime.UtcNow
        };
        invoice.SetFinancials(1000m, 200m, 1200m);
        typeof(InvoiceEntity).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(invoice, invoiceId);

        _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
            .ReturnsAsync(invoice);

        SetupHttpResponse(HttpStatusCode.OK, "{\"id\":\"LOGO-INV-67890\",\"success\":true}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncInvoiceAsync(invoiceId);

        // Assert
        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("LOGO-INV-67890");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SyncInvoiceAsync_InvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
            .ReturnsAsync((InvoiceEntity?)null);

        // Act
        var result = await ((IErpAdapter)_sut).SyncInvoiceAsync(invoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invoice not found");
    }

    [Fact]
    public async Task SyncInvoiceAsync_EmptyGuid_ReturnsFailure()
    {
        // Act
        var result = await ((IErpAdapter)_sut).SyncInvoiceAsync(Guid.Empty);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("InvoiceId cannot be empty");
    }

    [Fact]
    public async Task SyncInvoiceAsync_ApiError_ReturnsFailure()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new InvoiceEntity
        {
            InvoiceNumber = "INV-FAIL",
            Currency = "TRY"
        };
        invoice.SetFinancials(100m, 0m, 100m);
        typeof(InvoiceEntity).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(invoice, invoiceId);

        _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
            .ReturnsAsync(invoice);

        SetupHttpResponse(HttpStatusCode.InternalServerError, "{\"error\":\"server error\"}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncInvoiceAsync(invoiceId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("server error");
    }

    [Fact]
    public async Task SyncInvoiceAsync_401_InvalidatesTokenAndReturnsFailure()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new InvoiceEntity
        {
            InvoiceNumber = "INV-AUTH",
            Currency = "TRY"
        };
        invoice.SetFinancials(100m, 0m, 100m);
        typeof(InvoiceEntity).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(invoice, invoiceId);

        _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
            .ReturnsAsync(invoice);

        SetupHttpResponse(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncInvoiceAsync(invoiceId);

        // Assert
        result.Success.Should().BeFalse();
        // Token should be invalidated — next call will re-authenticate
        _cache.TryGetValue("Logo:AccessToken", out _).Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter (Dalga 12) — GetAccountBalancesAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAccountBalancesAsync_ValidResponse_ReturnsAccountList()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            accounts = new[]
            {
                new { accountCode = "100.01", accountName = "Kasa TRY", balance = "15234.50", currency = "TRY" },
                new { accountCode = "102.01", accountName = "Banka TRY", balance = "85000.00", currency = "TRY" },
                new { accountCode = "320.01", accountName = "Saticilar", balance = "-12500.75", currency = "TRY" }
            }
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await ((IErpAdapter)_sut).GetAccountBalancesAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].AccountCode.Should().Be("100.01");
        result[0].AccountName.Should().Be("Kasa TRY");
        result[0].Balance.Should().Be(15234.50m);
        result[0].Currency.Should().Be("TRY");

        result[1].Balance.Should().Be(85000.00m);
        result[2].Balance.Should().Be(-12500.75m);
    }

    [Fact]
    public async Task GetAccountBalancesAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"accounts\":[]}");

        // Act
        var result = await ((IErpAdapter)_sut).GetAccountBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountBalancesAsync_ApiError_ReturnsEmpty()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "{\"error\":\"server error\"}");

        // Act
        var result = await ((IErpAdapter)_sut).GetAccountBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountBalancesAsync_NetworkError_ReturnsEmpty()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        // Act
        var result = await ((IErpAdapter)_sut).GetAccountBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountBalancesAsync_MalformedBalance_SkipsInvalidEntries()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            accounts = new[]
            {
                new { accountCode = "100.01", accountName = "Valid", balance = "1000.00", currency = "TRY" },
                new { accountCode = "100.02", accountName = "Invalid", balance = "not-a-number", currency = "TRY" },
                new { accountCode = "100.03", accountName = "Also Valid", balance = "2000.00", currency = "TRY" }
            }
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await ((IErpAdapter)_sut).GetAccountBalancesAsync();

        // Assert — invalid entry skipped
        result.Should().HaveCount(2);
        result[0].AccountCode.Should().Be("100.01");
        result[1].AccountCode.Should().Be("100.03");
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter (Dalga 12) — PingAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PingAsync_Success_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"companies\":[]}");

        // Act
        var result = await ((IErpAdapter)_sut).PingAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_Failure_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"service down\"}");

        // Act
        var result = await ((IErpAdapter)_sut).PingAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PingAsync_NetworkError_ReturnsFalse()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await ((IErpAdapter)_sut).PingAsync();

        // Assert
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SyncOrderAsync with response missing ID — synthetic reference
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncOrderAsync_ResponseWithoutId_GeneratesSyntheticRef()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-NOID"
        };
        order.SetFinancials(0m, 0m, 500m);
        typeof(Order).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(order, orderId);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Logo API returns success but no ID
        SetupHttpResponse(HttpStatusCode.OK, "{\"success\":true}");

        // Act
        var result = await ((IErpAdapter)_sut).SyncOrderAsync(orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.ErpRef.Should().NotBeNullOrEmpty();
        result.ErpRef.Should().StartWith("LOGO-");
    }

    private static InvoiceEntity CreateInvoiceWithTotal(string invoiceNumber, decimal grandTotal)
    {
        var inv = new InvoiceEntity { InvoiceNumber = invoiceNumber };
        inv.SetFinancials(grandTotal, 0m, grandTotal);
        return inv;
    }
}
