using FluentAssertions;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateWarehouseHandler testi — depo güncelleme.
/// P1: Depo bilgileri stok yönetiminin temelidir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateWarehouseHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateWarehouseHandler CreateSut() => new(_warehouseRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_WarehouseNotFound_ShouldReturnFalse()
    {
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Warehouse?)null);
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "WH", "WH-1", null, "Main", true);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TenantMismatch_ShouldReturnFalse()
    {
        var wh = new Warehouse { Name = "Old", TenantId = Guid.NewGuid() };
        _warehouseRepo.Setup(r => r.GetByIdAsync(wh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var differentTenant = Guid.NewGuid();
        var cmd = new UpdateWarehouseCommand(differentTenant, wh.Id, "New", "NEW", null, "Main", true);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndReturnTrue()
    {
        var tenantId = Guid.NewGuid();
        var wh = new Warehouse { Name = "Old WH", Code = "OLD", TenantId = tenantId };
        _warehouseRepo.Setup(r => r.GetByIdAsync(wh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var cmd = new UpdateWarehouseCommand(tenantId, wh.Id, "Ana Depo", "AD-01", "Merkez depo", "Main", true);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        wh.Name.Should().Be("Ana Depo");
        wh.Code.Should().Be("AD-01");
        wh.Description.Should().Be("Merkez depo");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Deactivation_ShouldSetIsActiveFalse()
    {
        var tenantId = Guid.NewGuid();
        var wh = new Warehouse { Name = "WH", Code = "WH", IsActive = true, TenantId = tenantId };
        _warehouseRepo.Setup(r => r.GetByIdAsync(wh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var cmd = new UpdateWarehouseCommand(tenantId, wh.Id, "WH", "WH", null, "Main", false);
        await CreateSut().Handle(cmd, CancellationToken.None);

        wh.IsActive.Should().BeFalse();
    }
}
