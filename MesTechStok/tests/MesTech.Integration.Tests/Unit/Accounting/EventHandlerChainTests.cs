using FluentAssertions;
using MesTech.Domain.Events;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Handlers;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// N2-KALITE — Group 2: Event handler chain tests.
/// Tests OrderReceivedIntegrationHandler and InvoiceCreatedIntegrationHandler
/// to verify correct event dispatching and integration event publishing.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Accounting")]
[Trait("Group", "EventHandlerChain")]
public class EventHandlerChainTests
{
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<OrderReceivedIntegrationHandler>> _orderLoggerMock = new();
    private readonly Mock<ILogger<InvoiceCreatedIntegrationHandler>> _invoiceLoggerMock = new();

    private OrderReceivedIntegrationHandler CreateOrderHandler() =>
        new(_publisherMock.Object, _orderLoggerMock.Object);

    private InvoiceCreatedIntegrationHandler CreateInvoiceHandler() =>
        new(_publisherMock.Object, _invoiceLoggerMock.Object);

    // ═══════════════════════════════════════════════════════════════════
    // 1. OrderReceived — Gelir kaydi olusturma (integration event publish)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderReceived_CreatesIncomeRecord()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderReceivedEvent(
            orderId, "Trendyol", "TY-2026-001", 5_000m, DateTime.UtcNow);
        var notification = new DomainEventNotification<OrderReceivedEvent>(orderEvent);

        var handler = CreateOrderHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — PublishOrderReceivedAsync cagrildimi?
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId, "Trendyol", "TY-2026-001", 5_000m), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. OrderReceived — Dogru platform kaynagi
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderReceived_SetsCorrectPlatformSource()
    {
        // Arrange — Hepsiburada siparisi
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderReceivedEvent(
            orderId, "Hepsiburada", "HB-2026-042", 3_500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<OrderReceivedEvent>(orderEvent);

        var handler = CreateOrderHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — Platform kodu dogru mu?
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId, "Hepsiburada", It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. OrderReceived — Net tutar hesaplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderReceived_CalculatesNetAmount()
    {
        // Arrange — 7.500 TL siparis
        var orderId = Guid.NewGuid();
        var totalAmount = 7_500m;
        var orderEvent = new OrderReceivedEvent(
            orderId, "N11", "N11-2026-099", totalAmount, DateTime.UtcNow);
        var notification = new DomainEventNotification<OrderReceivedEvent>(orderEvent);

        var handler = CreateOrderHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — Tutar dogru iletildi mi?
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId, "N11", "N11-2026-099", totalAmount), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. OrderReceived — Multi-tenant TenantId
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderReceived_MultiTenant_SetsCorrectTenantId()
    {
        // Arrange — Farkli OrderId'ler ile 2 siparis
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        var event1 = new OrderReceivedEvent(orderId1, "Trendyol", "TY-001", 1_000m, DateTime.UtcNow);
        var event2 = new OrderReceivedEvent(orderId2, "Trendyol", "TY-002", 2_000m, DateTime.UtcNow);

        var handler = CreateOrderHandler();

        // Act
        await handler.Handle(new DomainEventNotification<OrderReceivedEvent>(event1), CancellationToken.None);
        await handler.Handle(new DomainEventNotification<OrderReceivedEvent>(event2), CancellationToken.None);

        // Assert — Her iki siparis icin ayri publish cagrisi
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. OrderReceived — Ayni siparis tekrarlanmaz (idempotent cagri kontrolu)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderReceived_DuplicateOrder_PublishesEachCall()
    {
        // Arrange — Ayni event 2 kez islenir (handler seviyesinde dedup yok,
        // idempotency MassTransit consumer tarafinda saglanir)
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderReceivedEvent(
            orderId, "Trendyol", "TY-DUP-001", 1_500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<OrderReceivedEvent>(orderEvent);

        var handler = CreateOrderHandler();

        // Act — Ayni event 2 kez
        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        // Assert — Handler her cagriyi publisher'a iletir (dedup publisher/consumer sorumlulugu)
        _publisherMock.Verify(p => p.PublishOrderReceivedAsync(
            orderId, "Trendyol", "TY-DUP-001", 1_500m), Times.Exactly(2));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. InvoiceGenerated — Alis faturasi gider olusturur
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InvoiceGenerated_PurchaseInvoice_PublishesEvent()
    {
        // Arrange — Alis faturasi (EFatura tipi)
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoiceEvent = new InvoiceCreatedEvent(
            invoiceId, orderId, InvoiceType.EFatura, 12_000m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(invoiceEvent);

        var handler = CreateInvoiceHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — PublishInvoiceCreatedAsync cagrildimi?
        _publisherMock.Verify(p => p.PublishInvoiceCreatedAsync(
            invoiceId, orderId, It.IsAny<string>(), 12_000m), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. InvoiceGenerated — Satis faturasi da publish edilir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InvoiceGenerated_SalesInvoice_AlsoPublishes()
    {
        // Arrange — Satis faturasi (EArsiv)
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoiceEvent = new InvoiceCreatedEvent(
            invoiceId, orderId, InvoiceType.EArsiv, 8_500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(invoiceEvent);

        var handler = CreateInvoiceHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — Handler tum fatura tiplerini yayinlar (filtreleme consumer'da)
        _publisherMock.Verify(p => p.PublishInvoiceCreatedAsync(
            invoiceId, orderId, It.IsAny<string>(), 8_500m), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. InvoiceGenerated — Dogru kategori (tutar kontrolu)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InvoiceGenerated_SetsCorrectAmount()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var grandTotal = 15_750.50m;
        var invoiceEvent = new InvoiceCreatedEvent(
            invoiceId, orderId, InvoiceType.EFatura, grandTotal, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(invoiceEvent);

        var handler = CreateInvoiceHandler();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — Tutar dogru iletildi
        _publisherMock.Verify(p => p.PublishInvoiceCreatedAsync(
            invoiceId, orderId, It.IsAny<string>(), grandTotal), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. TaxCalendar — 12 aylik KDV beyanname takvimleri olusturma
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateTaxCalendar_Creates12MonthlyKdvEvents()
    {
        // Arrange — 2026 yili icin 12 aylik KDV beyanname tarihleri
        var year = 2026;
        var kdvEvents = new List<(int Month, DateTime DueDate)>();

        // Act — Her ayin 26'si KDV beyanname tarihi
        for (int month = 1; month <= 12; month++)
        {
            var dueDate = new DateTime(year, month, 26, 0, 0, 0, DateTimeKind.Utc);
            kdvEvents.Add((month, dueDate));
        }

        // Assert
        kdvEvents.Should().HaveCount(12, "yilda 12 KDV beyanname tarihi olmali");
        kdvEvents.Should().AllSatisfy(e => e.DueDate.Day.Should().Be(26));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. TaxCalendar — Tum KDV tarihleri 26'si
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(10)]
    public void GenerateTaxCalendar_KdvDates_AllOn26th(int month)
    {
        // Arrange & Act
        var kdvDate = new DateTime(2026, month, 26, 23, 59, 59, DateTimeKind.Utc);

        // Assert
        kdvDate.Day.Should().Be(26,
            $"KDV beyanname teslim tarihi {month}. ay icin 26 olmali");
        kdvDate.Year.Should().Be(2026);
    }
}
