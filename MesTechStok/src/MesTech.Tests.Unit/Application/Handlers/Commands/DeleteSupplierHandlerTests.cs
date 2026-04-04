using FluentAssertions;
using MesTech.Application.Commands.DeleteSupplier;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class DeleteSupplierHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteSupplierHandler CreateHandler() =>
        new(_supplierRepo.Object, _unitOfWork.Object);

    [Fact]
    public void Constructor_NullSupplierRepository_ShouldThrow()
    {
        var act = () => new DeleteSupplierHandler(null!, _unitOfWork.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("supplierRepository");
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ShouldThrow()
    {
        var act = () => new DeleteSupplierHandler(_supplierRepo.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SupplierFound_ShouldDeleteAndReturnTrue()
    {
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier { Id = supplierId };
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(supplier);

        var handler = CreateHandler();
        var command = new DeleteSupplierCommand(supplierId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        _supplierRepo.Verify(r => r.DeleteAsync(supplierId), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SupplierNotFound_ShouldReturnFalse()
    {
        var supplierId = Guid.NewGuid();
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync((Supplier?)null);

        var handler = CreateHandler();
        var command = new DeleteSupplierCommand(supplierId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        _supplierRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
