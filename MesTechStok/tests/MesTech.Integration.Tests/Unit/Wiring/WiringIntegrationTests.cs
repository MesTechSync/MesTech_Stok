using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Wiring;

/// <summary>
/// Integration tests for 11 new wiring commits.
/// Verifies Handler→Service, Domain→Event, and ViewModel→Handler chains.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Wiring")]
[Trait("Group", "Integration")]
public class WiringIntegrationTests
{
    // ═══════════════════════════════════════
    // 1. FetchProductFromPlatformHandler wiring
    // ═══════════════════════════════════════

    [Fact]
    public async Task FetchProductFromPlatform_ValidUrl_ReturnsScrapedProduct()
    {
        // Arrange
        var scraperMock = new Mock<IProductScraperService>();
        var expected = new ScrapedProductDto(
            "Test Ürün", 199.90m, "https://img.test/1.jpg", "8680001234567",
            "trendyol", "Elektronik > Telefon", "TestBrand", "Açıklama");

        scraperMock.Setup(s => s.ScrapeFromUrlAsync("https://www.trendyol.com/p-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new FetchProductFromPlatformHandler(
            scraperMock.Object, Mock.Of<ILogger<FetchProductFromPlatformHandler>>());

        var query = new FetchProductFromPlatformQuery("https://www.trendyol.com/p-123");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Ürün");
        result.Price.Should().Be(199.90m);
        result.Platform.Should().Be("trendyol");
    }

    [Fact]
    public async Task FetchProductFromPlatform_ScraperReturnsNull_ReturnsNull()
    {
        var scraperMock = new Mock<IProductScraperService>();
        scraperMock.Setup(s => s.ScrapeFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScrapedProductDto?)null);

        var handler = new FetchProductFromPlatformHandler(
            scraperMock.Object, Mock.Of<ILogger<FetchProductFromPlatformHandler>>());

        var result = await handler.Handle(
            new FetchProductFromPlatformQuery("https://invalid.url/404"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchProductFromPlatform_NullRequest_Throws()
    {
        var handler = new FetchProductFromPlatformHandler(
            Mock.Of<IProductScraperService>(),
            Mock.Of<ILogger<FetchProductFromPlatformHandler>>());

        await Assert.ThrowsAnyAsync<Exception>(
            () => handler.Handle(null!, CancellationToken.None));
    }

    // ═══════════════════════════════════════
    // 2. FetchProductFromPlatformValidator wiring
    // ═══════════════════════════════════════

    [Fact]
    public void FetchProductFromPlatform_EmptyUrl_ValidationFails()
    {
        var validator = new FetchProductFromPlatformValidator();
        var result = validator.Validate(new FetchProductFromPlatformQuery(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FetchProductFromPlatform_InvalidUrl_ValidationFails()
    {
        var validator = new FetchProductFromPlatformValidator();
        var result = validator.Validate(new FetchProductFromPlatformQuery("not-a-url"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FetchProductFromPlatform_ValidHttpsUrl_ValidationPasses()
    {
        var validator = new FetchProductFromPlatformValidator();
        var result = validator.Validate(new FetchProductFromPlatformQuery("https://www.trendyol.com/p-123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FetchProductFromPlatform_FtpUrl_ValidationFails()
    {
        var validator = new FetchProductFromPlatformValidator();
        var result = validator.Validate(new FetchProductFromPlatformQuery("ftp://files.example.com/data"));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // 3. Customer.Create → CustomerCreatedEvent wiring
    // ═══════════════════════════════════════

    [Fact]
    public void Customer_Create_RaisesCustomerCreatedEvent()
    {
        var tenantId = Guid.NewGuid();
        var customer = Customer.Create(tenantId, "Test Müşteri", "C-001", "test@mail.com", "555-1234");

        customer.Should().NotBeNull();
        customer.Name.Should().Be("Test Müşteri");
        customer.Code.Should().Be("C-001");
        customer.TenantId.Should().Be(tenantId);

        // Domain event should be raised
        customer.DomainEvents.Should().ContainSingle(e => e is CustomerCreatedEvent);
        var evt = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        evt.TenantId.Should().Be(tenantId);
        evt.CustomerName.Should().Be("Test Müşteri");
    }

    [Fact]
    public void Customer_Create_EmptyName_Throws()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "", "C-001");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Customer_Create_EmptyCode_Throws()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "Valid Name", "");
        act.Should().Throw<Exception>();
    }

    // ═══════════════════════════════════════
    // 4. Order.MarkAsDelivered → OrderCompletedEvent wiring
    // ═══════════════════════════════════════

    [Fact]
    public void Order_MarkAsDelivered_FromShipped_RaisesOrderCompletedEvent()
    {
        // Arrange — create order and move to Shipped status
        var order = CreateShippedOrder();

        // Act
        order.MarkAsDelivered();

        // Assert
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
        order.DomainEvents.Should().Contain(e => e is OrderCompletedEvent);
    }

    [Fact]
    public void Order_MarkAsDelivered_NotShipped_Throws()
    {
        // Arrange — order in Processing status (not Shipped)
        var order = CreateProcessingOrder();

        // Act & Assert
        var act = () => order.MarkAsDelivered();
        act.Should().Throw<Exception>();
    }

    // ═══════════════════════════════════════
    // Helper methods
    // ═══════════════════════════════════════

    private static Order CreateShippedOrder()
    {
        var tenantId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, ProductId = Guid.NewGuid(), ProductName = "Test Ürün", ProductSKU = "TST-001", Quantity = 1, UnitPrice = 1000m, TotalPrice = 1000m }
        };

        var order = Order.CreateFromPlatform(
            tenantId,
            "ORD-001",
            PlatformType.Trendyol,
            "Müşteri Test",
            null,
            items);

        // Move through workflow: Pending → Confirmed → Shipped
        order.Place();
        order.MarkAsShipped("TRK-123", CargoProvider.YurticiKargo);
        return order;
    }

    private static Order CreateProcessingOrder()
    {
        var tenantId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, ProductId = Guid.NewGuid(), ProductName = "Test Ürün", ProductSKU = "TST-002", Quantity = 1, UnitPrice = 500m, TotalPrice = 500m }
        };

        var order = Order.CreateFromPlatform(tenantId, "ORD-002", PlatformType.Trendyol, "Test", null, items);
        order.Place(); // Pending → Confirmed (not Shipped)
        return order;
    }
}
