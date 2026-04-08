using FluentAssertions;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteWarehouseHandlerTests
{
    private readonly Mock<IWarehouseRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly DeleteWarehouseHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteWarehouseHandlerTests()
    {
        _repo = new Mock<IWarehouseRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new DeleteWarehouseHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidTenantAndWarehouse_ReturnsTrue()
    {
        var wh = new Warehouse { TenantId = _tenantId, Name = "Test" };
        var warehouseId = wh.Id;
        _repo.Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(_tenantId, warehouseId), CancellationToken.None);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(warehouseId, It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WarehouseNotFound_ReturnsFalse()
    {
        var warehouseId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>())).ReturnsAsync((Warehouse?)null);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(_tenantId, warehouseId), CancellationToken.None);

        result.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsFalse()
    {
        var otherTenant = Guid.NewGuid();
        var wh = new Warehouse { TenantId = otherTenant };
        var warehouseId = wh.Id;
        _repo.Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(_tenantId, warehouseId), CancellationToken.None);

        result.Should().BeFalse();
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
