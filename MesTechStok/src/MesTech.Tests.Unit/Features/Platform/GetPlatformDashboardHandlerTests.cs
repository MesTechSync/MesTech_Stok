using FluentAssertions;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Features.Platform;

[Trait("Category", "Unit")]
public class GetPlatformDashboardHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ILogger<GetPlatformDashboardHandler>> _loggerMock = new();
    private readonly GetPlatformDashboardHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetPlatformDashboardHandlerTests()
        => _sut = new GetPlatformDashboardHandler(
            _storeRepoMock.Object, _mappingRepoMock.Object,
            _orderRepoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_NoStoreForPlatform_ReturnsDisconnected()
    {
        _storeRepoMock.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        var query = new GetPlatformDashboardQuery(_tenantId, PlatformType.Trendyol);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsConnected.Should().BeFalse();
        result.SyncStatus.Should().Contain("yok");
    }

    [Fact]
    public async Task Handle_ActiveStore_ReturnsConnectedWithProducts()
    {
        var store = new Store
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            StoreName = "Trendyol Store",
            PlatformType = PlatformType.Trendyol,
            IsActive = true
        };

        _storeRepoMock.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store });
        _mappingRepoMock.Setup(r => r.CountByStoreIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetPlatformDashboardQuery(_tenantId, PlatformType.Trendyol);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsConnected.Should().BeTrue();
        result.ProductCount.Should().Be(42);
    }
}
