using FluentAssertions;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Tests.Integration._Shared;
using Moq;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// UnitOfWork integration tests — InMemory DB + Moq dispatcher.
/// Covers: SaveChanges with domain event dispatch, constructor guards, Dispose.
/// Transaction tests (Begin/Commit/Rollback) require a real provider (InMemory doesn't
/// support transactions) — those go in Testcontainers suite (5.06).
/// </summary>
[Trait("Category", "Integration")]
public class UnitOfWorkTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestCategoryId = Guid.NewGuid();

    private UnitOfWork CreateUnitOfWork(Mock<IDomainEventDispatcher>? dispatcherMock = null)
    {
        var dispatcher = dispatcherMock ?? new Mock<IDomainEventDispatcher>();
        return new UnitOfWork(Context, dispatcher.Object);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        var uow = CreateUnitOfWork();
        Context.Products.Add(new Product
        {
            Name = "UoW Test",
            SKU = "UOW-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        });

        var result = await uow.SaveChangesAsync();

        result.Should().Be(1);
        Context.Products.Should().ContainSingle(p => p.SKU == "UOW-001");
    }

    [Fact]
    public async Task SaveChangesAsync_WithDomainEvents_ShouldDispatchAfterSave()
    {
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        var capturedEvents = new List<IDomainEvent>();
        dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) =>
                capturedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var uow = CreateUnitOfWork(dispatcherMock);

        // AdjustStock raises StockChangedEvent
        var product = new Product
        {
            Name = "Event Product",
            SKU = "EVT-001",
            Stock = 100,
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(); // persist product first

        product.AdjustStock(-10, StockMovementType.Sale, "Test sale");

        var result = await uow.SaveChangesAsync();

        result.Should().BeGreaterThanOrEqualTo(0);
        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        capturedEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_NoDomainEvents_ShouldNotCallDispatcher()
    {
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        var uow = CreateUnitOfWork(dispatcherMock);

        Context.Products.Add(new Product
        {
            Name = "No Events",
            SKU = "NOEVT-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        });

        await uow.SaveChangesAsync();

        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleEntities_ShouldPersistAll()
    {
        var uow = CreateUnitOfWork();
        Context.Products.AddRange(
            new Product { Name = "Multi1", SKU = "MUL-001", CategoryId = TestCategoryId, TenantId = TestTenantId },
            new Product { Name = "Multi2", SKU = "MUL-002", CategoryId = TestCategoryId, TenantId = TestTenantId },
            new Product { Name = "Multi3", SKU = "MUL-003", CategoryId = TestCategoryId, TenantId = TestTenantId }
        );

        var result = await uow.SaveChangesAsync();

        result.Should().Be(3);
    }

    [Fact]
    public void Constructor_NullContext_ShouldThrow()
    {
        var dispatcher = new Mock<IDomainEventDispatcher>();

        var act = () => new UnitOfWork(null!, dispatcher.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_NullDispatcher_ShouldThrow()
    {
        var act = () => new UnitOfWork(Context, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dispatcher");
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var uow = CreateUnitOfWork();

        var act = () => uow.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var uow = CreateUnitOfWork();

        uow.Dispose();
        var act = () => uow.Dispose();

        act.Should().NotThrow();
    }
}
