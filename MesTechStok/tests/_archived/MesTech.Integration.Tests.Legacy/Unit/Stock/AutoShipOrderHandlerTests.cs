using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Interfaces;
using IAutoShipmentService = MesTech.Domain.Services.IAutoShipmentService;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// AutoShipOrderHandler: otomatik kargo oluşturma.
/// Kritik iş kuralları:
///   - Sipariş bulunmalı + tenant eşleşmeli
///   - Zaten kargoya verildiyse tekrar gönderilmemeli
///   - Kargo adapter factory'den resolve edilmeli
///   - Başarılı kargoda order shipped olarak işaretlenmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "ShippingChain")]
public class AutoShipOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IAutoShipmentService> _shipService = new();
    private readonly Mock<ICargoProviderFactory> _cargoFactory = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public AutoShipOrderHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _orderRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
    }

    private AutoShipOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _shipService.Object, _cargoFactory.Object, _uow.Object);

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var cmd = new AutoShipOrderCommand(Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
