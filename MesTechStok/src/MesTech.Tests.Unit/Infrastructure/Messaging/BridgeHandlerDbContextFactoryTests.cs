using FluentAssertions;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Handlers;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Messaging;

/// <summary>
/// KÖK-1 FIX doğrulama: 4 MediatR bridge handler IDbContextFactory pattern testi.
/// Commit 02d40336 — AppDbContext → IDbContextFactory geçişi.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class DealLostBridgeHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var publisher = new Mock<IMesaEventPublisher>();
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var tenant = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<DealLostBridgeHandler>>();

        var sut = new DealLostBridgeHandler(publisher.Object, factory.Object, tenant.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class DealWonBridgeHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var publisher = new Mock<IMesaEventPublisher>();
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var tenant = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<DealWonBridgeHandler>>();

        var sut = new DealWonBridgeHandler(publisher.Object, factory.Object, tenant.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class ExpensePaidHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var expenseRepo = new Mock<MesTech.Domain.Interfaces.IExpenseRepository>();
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var tenant = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<ExpensePaidHandler>>();

        var sut = new ExpensePaidHandler(expenseRepo.Object, factory.Object, tenant.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class LeadConvertedBridgeHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var publisher = new Mock<IMesaEventPublisher>();
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var tenant = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<LeadConvertedBridgeHandler>>();

        var sut = new LeadConvertedBridgeHandler(publisher.Object, factory.Object, tenant.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}
