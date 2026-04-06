using FluentAssertions;
using MesTech.Application.Commands.UpdateSupplier;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateSupplierHandler testi — tedarikçi güncelleme.
/// P1: Tedarikçi bilgileri stok + sipariş zincirinde kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateSupplierHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateSupplierHandler CreateSut() => new(_supplierRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_SupplierNotFound_ShouldReturnFailure()
    {
        _supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);
        var cmd = new UpdateSupplierCommand(Guid.NewGuid(), "Test", "TST");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndReturnSuccess()
    {
        var supplier = new Supplier { Name = "Old", Code = "OLD", TenantId = Guid.NewGuid() };
        _supplierRepo.Setup(r => r.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var cmd = new UpdateSupplierCommand(
            supplier.Id, "Yeni Tedarikçi", "YT-01",
            ContactPerson: "Mehmet",
            Email: "mehmet@supplier.com",
            Phone: "05559876543",
            City: "Ankara",
            TaxNumber: "9876543210",
            PaymentTermDays: 45);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SupplierId.Should().Be(supplier.Id);
        supplier.Name.Should().Be("Yeni Tedarikçi");
        supplier.ContactPerson.Should().Be("Mehmet");
        supplier.PaymentTermDays.Should().Be(45);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
