using FluentAssertions;
using MesTech.Application.Queries.GetSupplierById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetSupplierByIdHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepo = new();

    private GetSupplierByIdHandler CreateSut() => new(_supplierRepo.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new GetSupplierByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("supplierRepository");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SupplierExists_ShouldReturnSupplier()
    {
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            Code = "SUP-001",
            TenantId = Guid.NewGuid(),
            IsActive = true
        };
        // Set Id via reflection or property — BaseEntity has public Id
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(supplier, supplierId);

        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var sut = CreateSut();
        var result = await sut.Handle(new GetSupplierByIdQuery(supplierId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Supplier");
        result.Code.Should().Be("SUP-001");
    }

    [Fact]
    public async Task Handle_SupplierNotFound_ShouldReturnNull()
    {
        var supplierId = Guid.NewGuid();
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new GetSupplierByIdQuery(supplierId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        var supplierId = Guid.NewGuid();
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var sut = CreateSut();
        await sut.Handle(new GetSupplierByIdQuery(supplierId), CancellationToken.None);

        _supplierRepo.Verify(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
