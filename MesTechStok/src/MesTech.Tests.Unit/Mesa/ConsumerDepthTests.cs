using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// Consumer Depth Tests — I-13 S-11.
/// Deepened consumers inject repositories and do real work.
/// 2 tests per consumer (valid + missing data), 7 consumers = 14 tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ConsumerDepth")]
[Trait("Phase", "I-13")]
public class ConsumerDepthTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    private static ILogger<T> CreateLogger<T>() =>
        new Mock<ILogger<T>>().Object;

    private static Mock<ITenantProvider> CreateTenantProviderMock()
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return mock;
    }

    private static Mock<IProductRepository> CreateProductRepoMock(string sku = "SKU-TEST-001")
    {
        var mock = new Mock<IProductRepository>();
        var product = new Product { SKU = sku, Name = "Test Product", SalePrice = 100m };
        mock.Setup(x => x.GetBySKUAsync(sku)).ReturnsAsync(product);
        mock.Setup(x => x.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mock;
    }

    private static Mock<IOrderRepository> CreateOrderRepoMock(string orderNumber = "ORD-2026-001")
    {
        var mock = new Mock<IOrderRepository>();
        var order = new Order { OrderNumber = orderNumber };
        mock.Setup(x => x.GetByOrderNumberAsync(orderNumber)).ReturnsAsync(order);
        return mock;
    }

    // ══════════════════════════════════════════════
    //  1. MesaAiContentConsumer (deepened: +IProductRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiContentConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var productRepo = CreateProductRepoMock("SKU-DEPTH-001");
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiContentConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            TestProductId,
            "SKU-DEPTH-001",
            "AI generated product description for depth test",
            new Dictionary<string, string> { ["lang"] = "tr" },
            "GPT-4",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        productRepo.Verify(r => r.GetBySKUAsync("SKU-DEPTH-001"), Times.Once);
        productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    [Fact]
    public async Task AiContentConsumer_MissingSku_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiContentConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(),
            "",
            "Content for missing SKU",
            null,
            "GPT-4",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  2. MesaAiPriceConsumer (deepened: +IPriceRecommendationRepository, +IProductRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPriceConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var priceRepo = new Mock<IPriceRecommendationRepository>();
        priceRepo.Setup(x => x.AddAsync(It.IsAny<PriceRecommendation>())).Returns(Task.CompletedTask);
        var productRepo = CreateProductRepoMock("SKU-PRICE-D01");
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiPriceConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            priceRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            TestProductId,
            "SKU-PRICE-D01",
            149.99m,
            120.00m,
            180.00m,
            "Rakip analizi bazli oneri",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        priceRepo.Verify(r => r.AddAsync(It.IsAny<PriceRecommendation>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    [Fact]
    public async Task AiPriceConsumer_MissingSku_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var priceRepo = new Mock<IPriceRecommendationRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiPriceConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            priceRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(),
            "",
            99.99m,
            80.00m,
            120.00m,
            null,
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  3. MesaBotStatusConsumer (deepened: +INotificationLogRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotStatusConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(x => x.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotStatusConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            notifRepo.Object,
            uow.Object,
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp",
            "+905551234567",
            true,
            null,
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatusConsumer_EmptyChannel_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(x => x.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotStatusConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            notifRepo.Object,
            uow.Object,
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "",
            "",
            false,
            "Connection timeout",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  4. MesaAiPriceOptimizedConsumer (deepened: +IPriceRecommendationRepository, +IProductRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPriceOptimizedConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var priceRepo = new Mock<IPriceRecommendationRepository>();
        priceRepo.Setup(x => x.AddAsync(It.IsAny<PriceRecommendation>())).Returns(Task.CompletedTask);
        var productRepo = CreateProductRepoMock("SKU-OPT-D01");
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiPriceOptimizedConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            priceRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiPriceOptimizedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceOptimizedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceOptimizedEvent(
            TestProductId,
            "SKU-OPT-D01",
            139.90m,
            120.00m,
            160.00m,
            135.00m,
            0.85,
            "Buybox bazli optimizasyon",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        priceRepo.Verify(r => r.AddAsync(It.IsAny<PriceRecommendation>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.price.optimized"), Times.Once);
    }

    [Fact]
    public async Task AiPriceOptimizedConsumer_MissingSku_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var priceRepo = new Mock<IPriceRecommendationRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiPriceOptimizedConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            priceRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiPriceOptimizedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceOptimizedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceOptimizedEvent(
            Guid.NewGuid(),
            "",
            100.00m,
            80.00m,
            120.00m,
            null,
            0.50,
            null,
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.price.optimized"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  5. MesaAiStockPredictedConsumer (deepened: +IStockPredictionRepository, +IProductRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiStockPredictedConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var stockPredRepo = new Mock<IStockPredictionRepository>();
        stockPredRepo.Setup(x => x.AddAsync(It.IsAny<StockPrediction>())).Returns(Task.CompletedTask);
        var productRepo = CreateProductRepoMock("SKU-PRED-D01");
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiStockPredictedConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            stockPredRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiStockPredictedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiStockPredictedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiStockPredictedEvent(
            TestProductId,
            "SKU-PRED-D01",
            70,
            140,
            300,
            15,
            100,
            0.80,
            "Yeterli stok mevcut",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        stockPredRepo.Verify(r => r.AddAsync(It.IsAny<StockPrediction>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.stock.predicted"), Times.Once);
    }

    [Fact]
    public async Task AiStockPredictedConsumer_MissingSku_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var stockPredRepo = new Mock<IStockPredictionRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(x => x.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaAiStockPredictedConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            stockPredRepo.Object,
            productRepo.Object,
            uow.Object,
            CreateLogger<MesaAiStockPredictedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiStockPredictedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiStockPredictedEvent(
            Guid.NewGuid(),
            "",
            0,
            0,
            0,
            0,
            0,
            0.0,
            null,
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.stock.predicted"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  6. MesaBotInvoiceRequestConsumer (deepened: +IOrderRepository, +IInvoiceRepository)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotInvoiceRequestConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var orderRepo = CreateOrderRepoMock("ORD-INV-D01");
        var invoiceRepo = new Mock<IInvoiceRepository>();
        invoiceRepo.Setup(x => x.GetByOrderIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Invoice());
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotInvoiceRequestConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            orderRepo.Object,
            invoiceRepo.Object,
            uow.Object,
            CreateLogger<MesaBotInvoiceRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotInvoiceRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotInvoiceRequestedEvent(
            "+905551234567",
            "ORD-INV-D01",
            "WhatsApp",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        orderRepo.Verify(r => r.GetByOrderNumberAsync("ORD-INV-D01"), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.invoice.requested"), Times.Once);
    }

    [Fact]
    public async Task BotInvoiceRequestConsumer_MissingOrderNumber_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByOrderNumberAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotInvoiceRequestConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            orderRepo.Object,
            invoiceRepo.Object,
            uow.Object,
            CreateLogger<MesaBotInvoiceRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotInvoiceRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotInvoiceRequestedEvent(
            "+905559999999",
            "",
            "WhatsApp",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.invoice.requested"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  7. MesaBotReturnRequestConsumer (deepened: +IOrderRepository, +IReturnRequestRepository, +IUnitOfWork)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotReturnRequestConsumer_ValidEvent_CallsRepository()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var orderRepo = CreateOrderRepoMock("ORD-RET-D01");
        var returnRepo = new Mock<IReturnRequestRepository>();
        returnRepo.Setup(x => x.AddAsync(It.IsAny<ReturnRequest>())).Returns(Task.CompletedTask);
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotReturnRequestConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            orderRepo.Object,
            returnRepo.Object,
            uow.Object,
            CreateLogger<MesaBotReturnRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotReturnRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotReturnRequestedEvent(
            "+905559876543",
            "ORD-RET-D01",
            "Urun arizali",
            "WhatsApp",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        orderRepo.Verify(r => r.GetByOrderNumberAsync("ORD-RET-D01"), Times.Once);
        returnRepo.Verify(r => r.AddAsync(It.IsAny<ReturnRequest>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.return.requested"), Times.Once);
    }

    [Fact]
    public async Task BotReturnRequestConsumer_MissingOrderNumber_LogsWarningNoThrow()
    {
        // Arrange
        var monitor = CreateMonitor();
        var tenantProvider = CreateTenantProviderMock();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(x => x.GetByOrderNumberAsync(It.IsAny<string>())).ReturnsAsync((Order?)null);
        var returnRepo = new Mock<IReturnRequestRepository>();
        var uow = CreateUnitOfWorkMock();

        var consumer = new MesaBotReturnRequestConsumer(
            new Mock<IMediator>().Object,
            monitor.Object,
            tenantProvider.Object,
            orderRepo.Object,
            returnRepo.Object,
            uow.Object,
            CreateLogger<MesaBotReturnRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotReturnRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotReturnRequestedEvent(
            "+905551111111",
            "",
            null,
            "Telegram",
            TestTenantId,
            DateTime.UtcNow));

        // Act
        var act = async () => await consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.return.requested"), Times.Once);
    }
}
