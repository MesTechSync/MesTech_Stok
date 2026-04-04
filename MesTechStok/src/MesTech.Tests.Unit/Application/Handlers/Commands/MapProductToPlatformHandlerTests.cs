using FluentAssertions;
using MesTech.Application.Commands.MapProductToPlatform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: MapProductToPlatformHandler testi — ürün-platform eşleştirme.
/// P1: Eşleştirilmeyen ürün pazaryerinde listelenmez = satış kaybı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class MapProductToPlatformHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private MapProductToPlatformHandler CreateSut()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        return new(_productRepo.Object, _uow.Object, _tenantProvider.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldThrowKeyNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);
        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.Trendyol, "CAT-001");

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateMappingAndSave()
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        ProductPlatformMapping? captured = null;
        _productRepo.Setup(r => r.AddPlatformMappingAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPlatformMapping, CancellationToken>((m, _) => captured = m);

        var cmd = new MapProductToPlatformCommand(product.Id, PlatformType.Hepsiburada, "HB-CAT-123");
        await CreateSut().Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ProductId.Should().Be(product.Id);
        captured.PlatformType.Should().Be(PlatformType.Hepsiburada);
        captured.ExternalCategoryId.Should().Be("HB-CAT-123");
        captured.SyncStatus.Should().Be(SyncStatus.NotSynced);
        captured.IsEnabled.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
