using FluentAssertions;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 8: Bitrix24 CQRS Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class PushOrderToBitrix24HandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IBitrix24DealRepository> _dealRepo = new();
    private readonly Mock<IBitrix24Adapter> _adapter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PushOrderToBitrix24Handler CreateHandler() =>
        new(_orderRepo.Object, _dealRepo.Object, _adapter.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_OrderExists_NoDeal_ShouldPushAndCreateDeal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var externalDealId = "12345";
        var order = new Order { OrderNumber = "ORD-100" };
        order.SetFinancials(0m, 0m, 500m);
        EntityTestHelper.SetEntityId(order, orderId);

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalDealId);

        var handler = CreateHandler();
        var command = new PushOrderToBitrix24Command(orderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExternalDealId.Should().Be("12345");
        _dealRepo.Verify(r => r.AddAsync(It.IsAny<Bitrix24Deal>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        var handler = CreateHandler();
        var command = new PushOrderToBitrix24Command(orderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(orderId.ToString());
        _adapter.Verify(a => a.PushDealAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _dealRepo.Verify(r => r.AddAsync(It.IsAny<Bitrix24Deal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DealAlreadyExists_ShouldReturnExistingDeal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dealId = Guid.NewGuid();
        var existingDeal = new Bitrix24Deal
        {
            OrderId = orderId,
            ExternalDealId = "EXT-42",
            SyncStatus = SyncStatus.Synced
        };
        EntityTestHelper.SetEntityId(existingDeal, dealId);

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDeal);

        var handler = CreateHandler();
        var command = new PushOrderToBitrix24Command(orderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExternalDealId.Should().Be("EXT-42");
        result.Bitrix24DealId.Should().Be(dealId);
        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _adapter.Verify(a => a.PushDealAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdapterThrows_ShouldReturnErrorWithMessage()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { OrderNumber = "ORD-ERR" };
        order.SetFinancials(0m, 0m, 100m);
        EntityTestHelper.SetEntityId(order, orderId);

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bitrix24 API timeout"));

        var handler = CreateHandler();
        var command = new PushOrderToBitrix24Command(orderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Bitrix24 API timeout");
        _dealRepo.Verify(r => r.AddAsync(It.IsAny<Bitrix24Deal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdapterReturnsNull_ShouldReturnError()
    {
        // Arrange — adapter returns null string (push failed silently)
        var orderId = Guid.NewGuid();
        var order = new Order { OrderNumber = "ORD-NULL" };
        order.SetFinancials(0m, 0m, 250m);
        EntityTestHelper.SetEntityId(order, orderId);

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var handler = CreateHandler();
        var command = new PushOrderToBitrix24Command(orderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("null deal ID");
        _dealRepo.Verify(r => r.AddAsync(It.IsAny<Bitrix24Deal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

[Trait("Category", "Unit")]
public class SyncBitrix24ContactsHandlerTests
{
    private readonly Mock<IBitrix24ContactRepository> _contactRepo = new();
    private readonly Mock<IBitrix24Adapter> _adapter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SyncBitrix24ContactsHandler CreateHandler() =>
        new(_contactRepo.Object, _adapter.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SyncSucceeds_ShouldMarkContactsAsSynced()
    {
        // Arrange
        var contact1 = new Bitrix24Contact { Name = "Ali", SyncStatus = SyncStatus.NotSynced };
        var contact2 = new Bitrix24Contact { Name = "Veli", SyncStatus = SyncStatus.NotSynced };
        var unsyncedList = new List<Bitrix24Contact> { contact1, contact2 };

        _adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _contactRepo.Setup(r => r.GetUnsyncedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unsyncedList);

        var handler = CreateHandler();
        var command = new SyncBitrix24ContactsCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.SyncedCount.Should().Be(2);
        contact1.SyncStatus.Should().Be(SyncStatus.Synced);
        contact2.SyncStatus.Should().Be(SyncStatus.Synced);
        _contactRepo.Verify(
            r => r.UpdateAsync(It.IsAny<Bitrix24Contact>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoUnsyncedContacts_ShouldReturnZeroCount()
    {
        // Arrange
        _adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _contactRepo.Setup(r => r.GetUnsyncedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bitrix24Contact>());

        var handler = CreateHandler();
        var command = new SyncBitrix24ContactsCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.SyncedCount.Should().Be(0);
        _contactRepo.Verify(
            r => r.UpdateAsync(It.IsAny<Bitrix24Contact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdapterThrows_ShouldReturnErrorResult()
    {
        // Arrange
        _adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var handler = CreateHandler();
        var command = new SyncBitrix24ContactsCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("Connection refused");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

[Trait("Category", "Unit")]
public class GetBitrix24DealStatusHandlerTests
{
    private readonly Mock<IBitrix24DealRepository> _dealRepo = new();

    private GetBitrix24DealStatusHandler CreateHandler() =>
        new(_dealRepo.Object);

    [Fact]
    public async Task Handle_ExistingDeal_ShouldReturnDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dealId = Guid.NewGuid();
        var deal = new Bitrix24Deal
        {
            OrderId = orderId,
            ExternalDealId = "B24-1001",
            Title = "Order #ORD-50",
            Opportunity = 1500m,
            StageId = "WON",
            Currency = "TRY",
            SyncStatus = SyncStatus.Synced,
            LastSyncDate = new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc)
        };
        EntityTestHelper.SetEntityId(deal, dealId);

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var handler = CreateHandler();
        var query = new GetBitrix24DealStatusQuery(orderId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Bitrix24DealId.Should().Be(dealId);
        result.ExternalDealId.Should().Be("B24-1001");
        result.OrderId.Should().Be(orderId);
        result.Title.Should().Be("Order #ORD-50");
        result.Opportunity.Should().Be(1500m);
        result.StageId.Should().Be("WON");
        result.Currency.Should().Be("TRY");
        result.SyncStatus.Should().Be("Synced");
        result.LastSyncDate.Should().Be(new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc));
        _dealRepo.Verify(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoDeal_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);

        var handler = CreateHandler();
        var query = new GetBitrix24DealStatusQuery(orderId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
