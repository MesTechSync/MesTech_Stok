using FluentAssertions;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Application.Commands.UpdateSupplier;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Application.Queries.GetSuppliersPaged;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Supplier")]
[Trait("Group", "Handler")]
public class SupplierHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public SupplierHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact] public async Task CreateSupplier_Null_Throws() { var r = new Mock<ISupplierRepository>(); var h = new CreateSupplierHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateSupplier_Null_Throws() { var r = new Mock<ISupplierRepository>(); var h = new UpdateSupplierHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetSuppliers_Null_Throws() { var r = new Mock<ISupplierRepository>(); var h = new GetSuppliersHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetSuppliersPaged_Null_Throws() { var r = new Mock<ISupplierRepository>(); var h = new GetSuppliersPagedHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
