using FluentAssertions;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5 Batch 8: Bitrix24 handler testleri — PushOrderToBitrix24, SyncBitrix24Contacts.
/// P1: CRM entegrasyonu satış takibi için kritik.
/// </summary>

#region PushOrderToBitrix24

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class PushOrderToBitrix24HandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IBitrix24DealRepository> _dealRepo = new();
    private readonly Mock<IBitrix24Adapter> _adapter = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PushOrderToBitrix24Handler CreateSut() =>
        new(_orderRepo.Object, _dealRepo.Object, _adapter.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingDeal_ShouldReturnExistingWithoutPushing()
    {
        var existing = new Bitrix24Deal { ExternalDealId = "B24-123" };
        _dealRepo.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateSut().Handle(new PushOrderToBitrix24Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ExternalDealId.Should().Be("B24-123");
        _adapter.Verify(a => a.PushDealAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnError()
    {
        _dealRepo.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await CreateSut().Handle(new PushOrderToBitrix24Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldPushAndPersistDeal()
    {
        var order = FakeData.CreateOrder();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync("B24-NEW-456");

        var result = await CreateSut().Handle(new PushOrderToBitrix24Command(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ExternalDealId.Should().Be("B24-NEW-456");
        _dealRepo.Verify(r => r.AddAsync(It.IsAny<Bitrix24Deal>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdapterReturnsNull_ShouldReturnError()
    {
        var order = FakeData.CreateOrder();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CreateSut().Handle(new PushOrderToBitrix24Command(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("null");
    }

    [Fact]
    public async Task Handle_AdapterThrows_ShouldReturnErrorGracefully()
    {
        var order = FakeData.CreateOrder();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _adapter.Setup(a => a.PushDealAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bitrix24 API unavailable"));

        var result = await CreateSut().Handle(new PushOrderToBitrix24Command(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Bitrix24");
    }
}

#endregion

#region SyncBitrix24Contacts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SyncBitrix24ContactsHandlerTests
{
    private readonly Mock<IBitrix24ContactRepository> _contactRepo = new();
    private readonly Mock<IBitrix24Adapter> _adapter = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SyncBitrix24ContactsHandler CreateSut() =>
        new(_contactRepo.Object, _adapter.Object, _uow.Object);

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnSyncedCount()
    {
        _adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);
        _contactRepo.Setup(r => r.GetUnsyncedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bitrix24Contact>());

        var result = await CreateSut().Handle(new SyncBitrix24ContactsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SyncedCount.Should().Be(15);
    }

    [Fact]
    public async Task Handle_AdapterThrows_ShouldReturnErrorResult()
    {
        _adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection timeout"));

        var result = await CreateSut().Handle(new SyncBitrix24ContactsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("timeout"));
    }
}

#endregion
