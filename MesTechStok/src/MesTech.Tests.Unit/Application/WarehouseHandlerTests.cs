using FluentAssertions;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class WarehouseHandlerTests
{
    private readonly Mock<IWarehouseRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ── CreateWarehouse ──

    [Fact]
    public async Task CreateWarehouse_ValidCommand_ReturnsSuccess()
    {
        var cmd = new CreateWarehouseCommand("Ana Depo", "WH-001", "Istanbul", "Istanbul", true, Guid.NewGuid());
        var handler = new CreateWarehouseHandler(_repo.Object, _uow.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.WarehouseId.Should().NotBe(Guid.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWarehouse_EmptyName_ReturnsError()
    {
        var cmd = new CreateWarehouseCommand("", "WH-001");
        var handler = new CreateWarehouseHandler(_repo.Object, _uow.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── UpdateWarehouse ──

    [Fact]
    public async Task UpdateWarehouse_ValidCommand_UpdatesFields()
    {
        var tenantId = Guid.NewGuid();
        var warehouse = new Warehouse { TenantId = tenantId, Name = "Old" };
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(warehouse);

        var cmd = new UpdateWarehouseCommand(tenantId, warehouse.Id, "New Name", "WH-002", "Desc", "Main", true);
        var handler = new UpdateWarehouseHandler(_repo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        warehouse.Name.Should().Be("New Name");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWarehouse_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Warehouse?)null);
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "X", "X", null, "Main", true);
        var handler = new UpdateWarehouseHandler(_repo.Object, _uow.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().BeFalse();
    }

    // ── DeleteWarehouse ──

    [Fact]
    public async Task DeleteWarehouse_ValidCommand_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        var warehouse = new Warehouse { TenantId = tenantId };
        _repo.Setup(r => r.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(warehouse);

        var cmd = new DeleteWarehouseCommand(tenantId, warehouse.Id);
        var handler = new DeleteWarehouseHandler(_repo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(warehouse.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWarehouse_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Warehouse?)null);
        var cmd = new DeleteWarehouseCommand(Guid.NewGuid(), Guid.NewGuid());
        var handler = new DeleteWarehouseHandler(_repo.Object, _uow.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().BeFalse();
    }
}
