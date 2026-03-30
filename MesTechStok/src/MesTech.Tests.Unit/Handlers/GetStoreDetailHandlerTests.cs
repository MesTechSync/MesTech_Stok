using FluentAssertions;
using MesTech.Application.Features.Stores.Queries.GetStoreDetail;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStoreDetailHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credRepoMock = new();
    private readonly Mock<ILogger<GetStoreDetailHandler>> _loggerMock = new();
    private readonly GetStoreDetailHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStoreDetailHandlerTests()
    {
        _sut = new GetStoreDetailHandler(_storeRepoMock.Object, _credRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsNull()
    {
        _storeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var result = await _sut.Handle(
            new GetStoreDetailQuery(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsNull()
    {
        var store = new Store { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), StoreName = "Test" };
        _storeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var result = await _sut.Handle(
            new GetStoreDetailQuery(_tenantId, store.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidStore_ReturnsDetail()
    {
        var storeId = Guid.NewGuid();
        var store = new Store
        {
            Id = storeId,
            TenantId = _tenantId,
            StoreName = "Trendyol Mağaza",
            PlatformType = PlatformType.Trendyol,
            IsActive = true
        };
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { new() });

        var result = await _sut.Handle(
            new GetStoreDetailQuery(_tenantId, storeId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Trendyol Mağaza");
        result.Platform.Should().Be("Trendyol");
        result.CredentialStatus.Should().Be("Configured");
    }

    [Fact]
    public async Task Handle_NoCredentials_ShowsNotConfigured()
    {
        var storeId = Guid.NewGuid();
        var store = new Store
        {
            Id = storeId,
            TenantId = _tenantId,
            StoreName = "Test",
            PlatformType = PlatformType.N11,
            IsActive = false
        };
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var result = await _sut.Handle(
            new GetStoreDetailQuery(_tenantId, storeId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CredentialStatus.Should().Be("NotConfigured");
        result.WebhookStatus.Should().Be("Inactive");
    }
}
