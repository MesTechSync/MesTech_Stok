using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// GetPlatformListHandler unit testleri.
/// Tum PlatformType enum degerlerinin dondugunu ve store sayilarinin dogrulugunu test eder.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class GetPlatformListHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly GetPlatformListHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetPlatformListHandlerTests()
    {
        _handler = new GetPlatformListHandler(
            _storeRepoMock.Object,
            _adapterFactoryMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Tum PlatformType enum degerleri donmeli
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPlatformList_ReturnsAllPlatformTypes()
    {
        // Arrange
        _storeRepoMock
            .Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Store>());

        _adapterFactoryMock
            .Setup(f => f.Resolve(It.IsAny<PlatformType>()))
            .Returns((IIntegratorAdapter?)null);

        var expectedCount = Enum.GetValues<PlatformType>().Length;

        // Act
        var result = await _handler.Handle(
            new GetPlatformListQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(expectedCount);
        var platforms = result.Select(r => r.Platform).ToList();
        foreach (var pt in Enum.GetValues<PlatformType>())
        {
            platforms.Should().Contain(pt);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Store sayisi dogru hesaplanmali
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPlatformList_CountsStoresCorrectly()
    {
        // Arrange: 3 Trendyol store (2 aktif, 1 pasif)
        var stores = new List<Store>
        {
            new() { TenantId = _tenantId, PlatformType = PlatformType.Trendyol, StoreName = "Store1", IsActive = true },
            new() { TenantId = _tenantId, PlatformType = PlatformType.Trendyol, StoreName = "Store2", IsActive = true },
            new() { TenantId = _tenantId, PlatformType = PlatformType.Trendyol, StoreName = "Store3", IsActive = false },
        };

        _storeRepoMock
            .Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _adapterFactoryMock
            .Setup(f => f.Resolve(It.IsAny<PlatformType>()))
            .Returns((IIntegratorAdapter?)null);

        // Act
        var result = await _handler.Handle(
            new GetPlatformListQuery(_tenantId), CancellationToken.None);

        // Assert
        var trendyol = result.First(x => x.Platform == PlatformType.Trendyol);
        trendyol.StoreCount.Should().Be(3);
        trendyol.ActiveStoreCount.Should().Be(2);

        // Other platforms should have 0 stores
        var n11 = result.First(x => x.Platform == PlatformType.N11);
        n11.StoreCount.Should().Be(0);
        n11.ActiveStoreCount.Should().Be(0);
    }
}
