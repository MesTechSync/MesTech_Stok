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
    private readonly Mock<IStoreCredentialRepository> _credentialRepoMock = new();
    private readonly Mock<ILogger<GetStoreDetailHandler>> _loggerMock = new();
    private readonly GetStoreDetailHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _storeId = Guid.NewGuid();

    public GetStoreDetailHandlerTests()
    {
        _sut = new GetStoreDetailHandler(
            _storeRepoMock.Object,
            _credentialRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsNull()
    {
        // Arrange
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StoreBelongsToDifferentTenant_ReturnsNull()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var store = CreateStore(otherTenantId);

        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StoreWithCredentials_ReturnsConfiguredStatus()
    {
        // Arrange
        var store = CreateStore(_tenantId);
        var credentials = new List<StoreCredential>
        {
            new() { Id = Guid.NewGuid(), Key = "ApiKey", EncryptedValue = "enc-secret" }
        };

        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CredentialStatus.Should().Be("Configured");
        result.StoreId.Should().Be(_storeId);
        result.Name.Should().Be("Test Store");
        result.Platform.Should().Be("Trendyol");
        result.IsActive.Should().BeTrue();
        result.WebhookStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_StoreWithoutCredentials_ReturnsNotConfiguredStatus()
    {
        // Arrange
        var store = CreateStore(_tenantId);

        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CredentialStatus.Should().Be("NotConfigured");
    }

    [Fact]
    public async Task Handle_InactiveStore_ReturnsInactiveWebhookStatus()
    {
        // Arrange
        var store = CreateStore(_tenantId);
        store.IsActive = false;

        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        result.WebhookStatus.Should().Be("Inactive");
    }

    [Fact]
    public async Task Handle_StoreWithProductMappings_ReturnsCorrectCount()
    {
        // Arrange
        var store = CreateStore(_tenantId);
        store.ProductMappings = new List<ProductPlatformMapping>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var query = new GetStoreDetailQuery(_tenantId, _storeId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ProductCount.Should().Be(3);
    }

    private Store CreateStore(Guid tenantId) =>
        new()
        {
            Id = _storeId,
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
}
