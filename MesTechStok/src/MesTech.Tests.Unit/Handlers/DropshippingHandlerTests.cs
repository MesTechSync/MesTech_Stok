using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DropshippingHandlerTests
{
    // ── CreateDropshipSupplierHandler ──

    [Fact]
    public async Task CreateDropshipSupplier_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IDropshipSupplierRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateDropshipSupplierHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateDropshipSupplier_ValidRequest_AddsSupplierAndSaves()
    {
        var repo = new Mock<IDropshipSupplierRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateDropshipSupplierHandler(repo.Object, uow.Object);

        var command = new CreateDropshipSupplierCommand(
            Guid.NewGuid(), "TestSupplier", "https://example.com",
            DropshipMarkupType.Percentage, 15m);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<DropshipSupplier>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetDropshipSuppliersHandler ──

    [Fact]
    public async Task GetDropshipSuppliers_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IDropshipSupplierRepository>();
        var sut = new GetDropshipSuppliersHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetDropshipSuppliers_ValidRequest_ReturnsListFromRepo()
    {
        var repo = new Mock<IDropshipSupplierRepository>();
        var tenantId = Guid.NewGuid();
        repo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier>().AsReadOnly());

        var sut = new GetDropshipSuppliersHandler(repo.Object);
        var query = new GetDropshipSuppliersQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CreateAutoOrderHandler ──

    [Fact]
    public async Task CreateAutoOrder_NullRequest_ThrowsArgumentNullException()
    {
        var productRepo = new Mock<IProductRepository>();
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var orderRepo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAutoOrderHandler(
            productRepo.Object, supplierRepo.Object, orderRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAutoOrder_EmptyProductIds_ThrowsArgumentException()
    {
        var productRepo = new Mock<IProductRepository>();
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var orderRepo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateAutoOrderHandler(
            productRepo.Object, supplierRepo.Object, orderRepo.Object, uow.Object);

        var command = new CreateAutoOrderCommand(new List<Guid>(), Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAutoOrder_SupplierNotFound_ThrowsInvalidOperationException()
    {
        var productRepo = new Mock<IProductRepository>();
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var orderRepo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();

        supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipSupplier?)null);

        var sut = new CreateAutoOrderHandler(
            productRepo.Object, supplierRepo.Object, orderRepo.Object, uow.Object);

        var command = new CreateAutoOrderCommand(
            new List<Guid> { Guid.NewGuid() }, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── ExecuteBulkImportHandler ──

    [Fact]
    public async Task ExecuteBulkImport_NullRequest_ThrowsArgumentNullException()
    {
        var service = new Mock<IBulkProductImportService>();
        var sut = new ExecuteBulkImportHandler(service.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteBulkImport_InvalidExtension_ReturnsFailedResult()
    {
        var service = new Mock<IBulkProductImportService>();
        var sut = new ExecuteBulkImportHandler(service.Object);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new ExecuteBulkImportCommand(stream, "products.csv");

        var result = await sut.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.Failed);
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBulkImport_ValidXlsx_CallsImportService()
    {
        var service = new Mock<IBulkProductImportService>();
        var expected = new ImportResult(
            ImportStatus.Completed, 10, 10, 0, 0, 0, [], TimeSpan.FromSeconds(1));
        service.Setup(s => s.ImportProductsAsync(
            It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ExecuteBulkImportHandler(service.Object);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new ExecuteBulkImportCommand(stream, "products.xlsx");

        var result = await sut.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.Completed);
        service.Verify(s => s.ImportProductsAsync(
            It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
