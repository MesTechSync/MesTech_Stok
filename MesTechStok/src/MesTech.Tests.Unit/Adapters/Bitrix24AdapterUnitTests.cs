using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV 5 — Dalga 7.6 Task 5.02: Bitrix24Adapter unit tests.
/// Tests adapter logic, validation, resilience, and rate limiting.
/// Complements the 23 WireMock integration tests in Tests.Integration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Bitrix24")]
public class Bitrix24AdapterUnitTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Bitrix24Adapter> _logger;
    private readonly Bitrix24Adapter _sut;

    public Bitrix24AdapterUnitTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://test-portal.bitrix24.com/rest/")
        };
        _logger = NullLogger<Bitrix24Adapter>.Instance;
        _sut = new Bitrix24Adapter(_httpClient, _logger);
    }

    #region Constructor & Contract Tests

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new Bitrix24Adapter(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new Bitrix24Adapter(_httpClient, null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void PlatformCode_Returns_Bitrix24()
    {
        _sut.PlatformCode.Should().Be("Bitrix24");
    }

    [Fact]
    public void SupportsStockUpdate_ReturnsFalse()
    {
        _sut.SupportsStockUpdate.Should().BeFalse(
            "Bitrix24 is CRM-focused, no native stock management");
    }

    [Fact]
    public void SupportsPriceUpdate_ReturnsTrue()
    {
        _sut.SupportsPriceUpdate.Should().BeTrue(
            "Bitrix24 CRM product catalog supports price fields");
    }

    [Fact]
    public void SupportsShipment_ReturnsFalse()
    {
        _sut.SupportsShipment.Should().BeFalse(
            "Bitrix24 CRM does not have cargo/shipment management");
    }

    #endregion

    #region PushStockUpdateAsync — Always False

    [Fact]
    public async Task PushStockUpdateAsync_AlwaysReturnsFalse()
    {
        // Bitrix24 does not support stock updates — should return false without HTTP call
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 100);

        result.Should().BeFalse();

        // Verify NO HTTP call was made
        _mockHandler.Protected()
            .Verify("SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region TestConnectionAsync — Credential Validation

    [Fact]
    public async Task TestConnectionAsync_NullCredentials_ThrowsArgumentNullException()
    {
        var act = () => _sut.TestConnectionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TestConnectionAsync_MissingClientId_ReturnsFailure()
    {
        var creds = new Dictionary<string, string>
        {
            ["Bitrix24ClientSecret"] = "secret",
            ["Bitrix24PortalDomain"] = "test.bitrix24.com",
            ["Bitrix24RefreshToken"] = "refresh"
        };

        var result = await _sut.TestConnectionAsync(creds);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Bitrix24ClientId");
    }

    [Fact]
    public async Task TestConnectionAsync_MissingPortalDomain_ReturnsFailure()
    {
        var creds = new Dictionary<string, string>
        {
            ["Bitrix24ClientId"] = "id",
            ["Bitrix24ClientSecret"] = "secret",
            ["Bitrix24RefreshToken"] = "refresh"
        };

        var result = await _sut.TestConnectionAsync(creds);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Bitrix24PortalDomain");
    }

    [Theory]
    [InlineData("Bitrix24ClientId")]
    [InlineData("Bitrix24ClientSecret")]
    [InlineData("Bitrix24PortalDomain")]
    [InlineData("Bitrix24RefreshToken")]
    public async Task TestConnectionAsync_EmptyRequiredField_ReturnsFailure(string emptyField)
    {
        var creds = new Dictionary<string, string>
        {
            ["Bitrix24ClientId"] = "id",
            ["Bitrix24ClientSecret"] = "secret",
            ["Bitrix24PortalDomain"] = "test.bitrix24.com",
            ["Bitrix24RefreshToken"] = "refresh"
        };
        creds[emptyField] = "";

        var result = await _sut.TestConnectionAsync(creds);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(emptyField);
    }

    [Fact]
    public async Task TestConnectionAsync_ResultContainsPlatformCode()
    {
        var creds = new Dictionary<string, string>
        {
            ["Bitrix24ClientId"] = "id",
            ["Bitrix24ClientSecret"] = "secret"
            // Missing others — will fail fast
        };

        var result = await _sut.TestConnectionAsync(creds);

        result.PlatformCode.Should().Be("Bitrix24");
    }

    [Fact]
    public async Task TestConnectionAsync_Failure_HasResponseTime()
    {
        var creds = new Dictionary<string, string>
        {
            ["Bitrix24ClientId"] = "id"
            // Missing required fields
        };

        var result = await _sut.TestConnectionAsync(creds);

        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region EnsureConfigured — Unconfigured Guard

    [Fact]
    public async Task PushProductAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var product = new Product { Name = "Test", SalePrice = 100m };

        var act = () => _sut.PushProductAsync(product);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task PullProductsAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.PullProductsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task PushDealAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var order = CreateTestOrder();

        var act = () => _sut.PushDealAsync(order);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task SyncContactsAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.SyncContactsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task UpdateDealStageAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.UpdateDealStageAsync("123", "WON");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task BatchExecuteAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.BatchExecuteAsync(new[] { "crm.deal.list" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task GetCatalogProductsAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.GetCatalogProductsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task RegisterWebhookAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var act = () => _sut.RegisterWebhookAsync("https://callback.example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    #endregion

    #region ConfigureRateLimit — Static Method

    [Fact]
    public void ConfigureRateLimit_SetsNewConcurrency()
    {
        // Enterprise default is 50 — reconfigure to Free tier (2)
        Bitrix24Adapter.ConfigureRateLimit(2);

        // Verify via reflection
        var semaphoreField = typeof(Bitrix24Adapter)
            .GetField("_rateLimitSemaphore", BindingFlags.Static | BindingFlags.NonPublic);
        semaphoreField.Should().NotBeNull();

        var semaphore = semaphoreField!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull();
        semaphore!.CurrentCount.Should().Be(2);

        // Restore default for other tests
        Bitrix24Adapter.ConfigureRateLimit(50);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void ConfigureRateLimit_VariousLimits_ReflectedInSemaphore(int limit)
    {
        Bitrix24Adapter.ConfigureRateLimit(limit);

        var semaphoreField = typeof(Bitrix24Adapter)
            .GetField("_rateLimitSemaphore", BindingFlags.Static | BindingFlags.NonPublic);
        var semaphore = (SemaphoreSlim)semaphoreField!.GetValue(null)!;

        semaphore.CurrentCount.Should().Be(limit);

        // Restore default
        Bitrix24Adapter.ConfigureRateLimit(50);
    }

    #endregion

    #region MapOrderStatusToStage — via Reflection

    [Theory]
    [InlineData(OrderStatus.Pending, "NEW")]
    [InlineData(OrderStatus.Confirmed, "PREPARATION")]
    [InlineData(OrderStatus.Shipped, "EXECUTING")]
    [InlineData(OrderStatus.Delivered, "WON")]
    [InlineData(OrderStatus.Cancelled, "LOSE")]
    public void MapOrderStatusToStage_CorrectMapping(OrderStatus status, string expectedStage)
    {
        var method = typeof(Bitrix24Adapter)
            .GetMethod("MapOrderStatusToStage", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull("MapOrderStatusToStage should exist as private static");

        var result = method!.Invoke(null, new object[] { status }) as string;

        result.Should().Be(expectedStage);
    }

    #endregion

    #region Interface Contracts

    [Fact]
    public void Bitrix24Adapter_Implements_IBitrix24Adapter()
    {
        _sut.Should().BeAssignableTo<IBitrix24Adapter>();
    }

    [Fact]
    public void Bitrix24Adapter_Implements_IWebhookCapableAdapter()
    {
        _sut.Should().BeAssignableTo<IWebhookCapableAdapter>();
    }

    [Fact]
    public void Bitrix24Adapter_Implements_IIntegratorAdapter()
    {
        _sut.Should().BeAssignableTo<IIntegratorAdapter>();
    }

    [Fact]
    public void IBitrix24Adapter_HasAllCrmMethods()
    {
        var interfaceType = typeof(IBitrix24Adapter);

        interfaceType.GetMethod("PushDealAsync").Should().NotBeNull();
        interfaceType.GetMethod("SyncContactsAsync").Should().NotBeNull();
        interfaceType.GetMethod("GetCatalogProductsAsync").Should().NotBeNull();
        interfaceType.GetMethod("UpdateDealStageAsync").Should().NotBeNull();
        interfaceType.GetMethod("BatchExecuteAsync").Should().NotBeNull();
    }

    #endregion

    #region Polly Pipeline — Retry & Circuit Breaker Config Verification

    [Fact]
    public void RetryPipeline_ExistsInAdapter()
    {
        var pipelineField = typeof(Bitrix24Adapter)
            .GetField("_retryPipeline", BindingFlags.Instance | BindingFlags.NonPublic);
        pipelineField.Should().NotBeNull("adapter must have a Polly resilience pipeline");

        var pipeline = pipelineField!.GetValue(_sut);
        pipeline.Should().NotBeNull("pipeline should be initialized in constructor");
    }

    [Fact]
    public void MaxBatchCommands_Is50()
    {
        var field = typeof(Bitrix24Adapter)
            .GetField("MaxBatchCommands", BindingFlags.Static | BindingFlags.NonPublic);
        field.Should().NotBeNull();

        var value = (int)field!.GetValue(null)!;
        value.Should().Be(50, "Bitrix24 batch API allows max 50 commands per request");
    }

    #endregion

    #region ProcessWebhookPayloadAsync

    [Fact]
    public async Task ProcessWebhookPayloadAsync_ValidPayload_ReturnsTrue()
    {
        // ProcessWebhookPayloadAsync does NOT require configuration
        var payload = "event=ONCRMDEALADD&data%5BFIELDS%5D%5BID%5D=123&auth%5Bapplication_token%5D=abc";

        var result = await _sut.ProcessWebhookPayloadAsync(payload, null);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWebhookPayloadAsync_NullPayload_ReturnsTrue()
    {
        // Adapter logs and returns true even for null (defensive)
        var result = await _sut.ProcessWebhookPayloadAsync(null!, null);

        result.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private static Order CreateTestOrder()
    {
        var order = new Order
        {
            OrderNumber = "MES-2026-001",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Confirmed,
            OrderDate = new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc)
        };
        order.SetFinancials(0m, 0m, 1250.00m);
        return order;
    }

    #endregion
}
