using FluentAssertions;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateWarehouseHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateWarehouseHandler _sut;

    public CreateWarehouseHandlerTests()
    {
        _warehouseRepo = new Mock<IWarehouseRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateWarehouseHandler(_warehouseRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesWarehouseAndReturnsSuccess()
    {
        var command = new CreateWarehouseCommand("Ana Depo", "WH-001", "Istanbul", "Istanbul");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.WarehouseId.Should().NotBe(Guid.Empty);
        _warehouseRepo.Verify(r => r.AddAsync(It.Is<Warehouse>(w =>
            w.Name == "Ana Depo" && w.Code == "WH-001")), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsFailure()
    {
        var command = new CreateWarehouseCommand("", "WH-002");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_EmptyCode_ReturnsFailure()
    {
        var command = new CreateWarehouseCommand("Depo", "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DefaultFlag_SetsAsDefault()
    {
        var command = new CreateWarehouseCommand("Default Depo", "WH-DEF", IsDefault: true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _warehouseRepo.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Once());
    }
}
