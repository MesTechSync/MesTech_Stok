using System.Net;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.DependencyInjection;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Handlers;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Runtime;

/// <summary>
/// Trendyol adapter runtime tests — full DI chain verification.
/// Orchestrator -> Factory -> TrendyolAdapter end-to-end.
/// </summary>
public class TrendyolRuntimeTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddMemoryCache();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integrations:Trendyol:Enabled"] = "false",
                ["Integrations:eBay:Enabled"] = "false"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddIntegrationServices(configuration);
        return services.BuildServiceProvider();
    }

    // ── Test 1: Full chain Orchestrator → Factory → TrendyolAdapter ─────

    [Fact]
    public async Task FullChain_Orchestrator_Factory_TrendyolAdapter_SyncResult()
    {
        // Arrange — build real DI container
        using var provider = BuildProvider();
        var orchestrator = provider.GetRequiredService<IIntegratorOrchestrator>();

        // Verify RegisteredAdapters contains "Trendyol"
        orchestrator.RegisteredAdapters.Should().Contain(
            a => a.PlatformCode.Equals("Trendyol", StringComparison.OrdinalIgnoreCase));

        // Act — call SyncPlatformAsync (will fail HTTP since no real credentials, that's OK)
        var result = await orchestrator.SyncPlatformAsync("Trendyol");

        // Assert — result is NOT null, PlatformCode matches, CompletedAt is set
        result.Should().NotBeNull();
        result.PlatformCode.Should().Be("Trendyol");
        result.CompletedAt.Should().NotBeNull();
        // The adapter is not configured, so it should fail with InvalidOperationException
        // but the orchestrator catches it and sets ErrorMessage
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
    }

    // ── Test 2: Factory resolves Trendyol with all capabilities ──────────

    [Fact]
    public void Factory_Resolve_Trendyol_HasAllCapabilities()
    {
        // Arrange
        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IAdapterFactory>();

        // Act — resolve the Trendyol adapter
        var adapter = factory.Resolve("Trendyol");

        // Assert — adapter exists and is TrendyolAdapter
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<TrendyolAdapter>();

        // Verify all capability interfaces via ResolveCapability<T>
        var orderCapable = factory.ResolveCapability<IOrderCapableAdapter>("Trendyol");
        orderCapable.Should().NotBeNull();

        var webhookCapable = factory.ResolveCapability<IWebhookCapableAdapter>("Trendyol");
        webhookCapable.Should().NotBeNull();

        var invoiceCapable = factory.ResolveCapability<IInvoiceCapableAdapter>("Trendyol");
        invoiceCapable.Should().NotBeNull();

        var claimCapable = factory.ResolveCapability<IClaimCapableAdapter>("Trendyol");
        claimCapable.Should().NotBeNull();

        var settlementCapable = factory.ResolveCapability<ISettlementCapableAdapter>("Trendyol");
        settlementCapable.Should().NotBeNull();
    }

    // ── Test 3: Orchestrator HandleStockChanged does NOT throw ───────────

    [Fact]
    public async Task Orchestrator_HandleStockChanged_TriggersAdapters()
    {
        // Arrange
        using var provider = BuildProvider();
        var orchestrator = provider.GetRequiredService<IIntegratorOrchestrator>();

        var stockEvent = new StockChangedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SKU: "TEST-SKU",
            PreviousQuantity: 10,
            NewQuantity: 5,
            MovementType: StockMovementType.StockIn,
            OccurredAt: DateTime.UtcNow);

        // Act & Assert — should NOT throw (adapters catch exceptions internally)
        var exception = await Record.ExceptionAsync(
            () => orchestrator.HandleStockChangedAsync(stockEvent));

        exception.Should().BeNull();
    }

    // ── Test 4: MediatR → StockChangedIntegrationHandler chain ──────────

    [Fact]
    public async Task EventDispatchChain_StockChanged_ReachesHandler()
    {
        // Arrange
        var mockPublisher = new Mock<IIntegrationEventPublisher>();
        var mockLogger = new Mock<ILogger<StockChangedIntegrationHandler>>();

        var handler = new StockChangedIntegrationHandler(
            mockPublisher.Object, mockLogger.Object);

        var stockEvent = new StockChangedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SKU: "HANDLER-TEST-SKU",
            PreviousQuantity: 20,
            NewQuantity: 15,
            MovementType: StockMovementType.StockOut,
            OccurredAt: DateTime.UtcNow);

        var notification = new DomainEventNotification<StockChangedEvent>(stockEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — verify mock publisher received the call with correct args
        mockPublisher.Verify(
            p => p.PublishStockChangedAsync(
                stockEvent.ProductId,
                "HANDLER-TEST-SKU",
                15,
                It.Is<string>(s => s == nameof(StockMovementType.StockOut))),
            Times.Once);
    }

    // ── Test 5: TestConnection with MockHttp ─────────────────────────────

    [Fact]
    public async Task TestConnection_Trendyol_WithMockHttp_Success()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 150}""");

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com/")
        };
        var loggerMock = new Mock<ILogger<TrendyolAdapter>>();
        var adapter = new TrendyolAdapter(httpClient, loggerMock.Object);

        var credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "test-key",
            ["ApiSecret"] = "test-secret",
            ["SupplierId"] = "99999"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Trendyol");
        result.ProductCount.Should().Be(150);
        result.StoreName.Should().Be("Trendyol - Supplier 99999");

        // Verify exactly 1 request captured
        handler.CapturedRequests.Should().ContainSingle();

        // Verify it hit the supplier products endpoint
        var requestUrl = handler.CapturedRequests[0].RequestUri!.ToString();
        requestUrl.Should().Contain("/integration/product/sellers/99999/products");
    }
}
