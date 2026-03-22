using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class SyncDropshipProductsHandlerTests
{
    private readonly Mock<IDropshipSupplierRepository> _supplierRepo = new();
    private readonly Mock<IDropshipProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDropshipFeedFetcher> _feedFetcher = new();
    private readonly Mock<ILogger<SyncDropshipProductsHandler>> _logger = new();

    private SyncDropshipProductsHandler CreateSut() =>
        new(_supplierRepo.Object, _productRepo.Object, _uow.Object, _feedFetcher.Object, _logger.Object);

    private static DropshipSupplier CreateSupplier(Guid tenantId, bool withEndpoint = true)
    {
        var supplier = DropshipSupplier.Create(
            tenantId, "Test Supplier", "https://supplier.com",
            DropshipMarkupType.Percentage, 15m);
        if (withEndpoint)
            supplier.SetApiCredentials("https://api.supplier.com/feed", "test-key");
        return supplier;
    }

    [Fact]
    public async Task Handle_ValidRequest_WithNewProducts_ReturnsCreatedCount()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier = CreateSupplier(tenantId);

        _supplierRepo
            .Setup(r => r.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        _productRepo
            .Setup(r => r.GetBySupplierAsync(supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipProduct>().AsReadOnly());

        var feedItems = new List<DropshipFeedItem>
        {
            new("EXT-001", "Product A", 100m, 50),
            new("EXT-002", "Product B", 200m, 30)
        };
        _feedFetcher
            .Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItems.AsReadOnly());

        var command = new SyncDropshipProductsCommand(tenantId, supplier.Id);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _productRepo.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<DropshipProduct>>(list => list.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SupplierNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        _supplierRepo
            .Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipSupplier?)null);

        var command = new SyncDropshipProductsCommand(tenantId, supplierId);
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Supplier not found*");
    }

    [Fact]
    public async Task Handle_SupplierTenantMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var supplier = CreateSupplier(differentTenantId);

        _supplierRepo
            .Setup(r => r.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var command = new SyncDropshipProductsCommand(tenantId, supplier.Id);
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong*");
    }

    [Fact]
    public async Task Handle_NoApiEndpoint_RecordsSyncAndReturnsZero()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier = CreateSupplier(tenantId, withEndpoint: false);

        _supplierRepo
            .Setup(r => r.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var command = new SyncDropshipProductsCommand(tenantId, supplier.Id);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _feedFetcher.Verify(
            f => f.FetchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyFeed_RecordsSyncAndReturnsZero()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier = CreateSupplier(tenantId);

        _supplierRepo
            .Setup(r => r.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        _feedFetcher
            .Setup(f => f.FetchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipFeedItem>().AsReadOnly());

        var command = new SyncDropshipProductsCommand(tenantId, supplier.Id);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
