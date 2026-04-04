using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: CreateOrderHandler testi — manuel sipariş oluşturma.
/// P1: Sipariş oluşturma tüm iş akışının başlangıç noktası.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private CreateOrderHandler CreateSut()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        return new(_orderRepo.Object, _uow.Object, _tenantProvider.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateOrderAndSave()
    {
        Order? captured = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => captured = o);

        var cmd = new CreateOrderCommand(Guid.NewGuid(), "Test Müşteri", "test@test.com", "Manual", "Test sipariş");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        captured!.CustomerName.Should().Be("Test Müşteri");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetTenantFromProvider()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(tenantId);

        Order? captured = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => captured = o);

        var sut = new CreateOrderHandler(_orderRepo.Object, _uow.Object, _tenantProvider.Object);
        var cmd = new CreateOrderCommand(Guid.NewGuid(), "Müşteri", null, "Manual", null);
        await sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_EmptyCustomerName_ShouldThrow()
    {
        var cmd = new CreateOrderCommand(Guid.NewGuid(), "", null, "Manual", null);

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
