using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Platform;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: Platform Handler Tests
// ════════════════════════════════════════════════════════

#region CreateStoreHandler

[Trait("Category", "Unit")]
public class CreateStoreHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepo = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionService = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CreateStoreHandler>> _logger = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CreateStoreHandler CreateHandler() =>
        new(_storeRepo.Object, _credentialRepo.Object, _encryptionService.Object,
            _adapterFactory.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateStoreAndReturnSuccess()
    {
        // Arrange
        var credentials = new Dictionary<string, string> { ["apiKey"] = "test-key" };
        _encryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted");
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();
        var command = new CreateStoreCommand(_tenantId, "My Store", PlatformType.Trendyol, credentials);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoreId.Should().NotBeNull();
        _storeRepo.Verify(r => r.AddAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyStoreName_ShouldReturnError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateStoreCommand(_tenantId, "", PlatformType.Trendyol, new Dictionary<string, string>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task Handle_ConnectionTestFails_ShouldRollbackAndReturnError()
    {
        // Arrange
        var credentials = new Dictionary<string, string> { ["apiKey"] = "bad-key" };
        _encryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted");

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = false, ErrorMessage = "Invalid API key" });

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Hepsiburada)).Returns(mockAdapter.Object);

        var handler = CreateHandler();
        var command = new CreateStoreCommand(_tenantId, "HB Store", PlatformType.Hepsiburada, credentials);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection test failed");
        _storeRepo.Verify(r => r.DeleteAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region FetchProductFromUrlHandler

[Trait("Category", "Unit")]
public class FetchProductFromUrlHandlerTests
{
    private readonly Mock<ILogger<FetchProductFromUrlHandler>> _logger = new();

    private FetchProductFromUrlHandler CreateHandler() => new(_logger.Object);

    [Fact]
    public async Task Handle_TrendyolUrl_ShouldIdentifyPlatform()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new FetchProductFromUrlCommand("https://www.trendyol.com/p/product-12345");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Attributes.Should().ContainKey("Platform");
        result.Attributes["Platform"].Should().Be("Trendyol");
    }

    [Fact]
    public async Task Handle_EmptyUrl_ShouldReturnNull()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new FetchProductFromUrlCommand("");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InvalidUrl_ShouldReturnNull()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new FetchProductFromUrlCommand("not-a-valid-url");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnknownDomain_ShouldReturnNull()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new FetchProductFromUrlCommand("https://www.unknown-marketplace.com/product/123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region TestStoreConnectionHandler

[Trait("Category", "Unit")]
public class TestStoreConnectionHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepo = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionService = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ILogger<TestStoreConnectionHandler>> _logger = new();

    private TestStoreConnectionHandler CreateHandler() =>
        new(_storeRepo.Object, _credentialRepo.Object, _encryptionService.Object,
            _adapterFactory.Object, _logger.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ShouldReturnFailure()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        _storeRepo.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var command = new TestStoreConnectionCommand(storeId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoAdapterAvailable_ShouldReturnFailure()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var store = FakeData.CreateStore(Guid.NewGuid(), PlatformType.Ozon);

        _storeRepo.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepo.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>().AsReadOnly());
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Ozon))
            .Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();
        var command = new TestStoreConnectionCommand(storeId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No adapter");
    }

    [Fact]
    public async Task Handle_SuccessfulConnection_ShouldReturnSuccess()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var store = FakeData.CreateStore(Guid.NewGuid(), PlatformType.Trendyol);

        _storeRepo.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepo.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>().AsReadOnly());

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = true });
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(mockAdapter.Object);

        var handler = CreateHandler();
        var command = new TestStoreConnectionCommand(storeId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetPlatformListHandler

[Trait("Category", "Unit")]
public class GetPlatformListHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetPlatformListHandler CreateHandler() =>
        new(_storeRepo.Object, _adapterFactory.Object);

    [Fact]
    public async Task Handle_NoStores_ShouldReturnAllPlatformsWithZeroCounts()
    {
        // Arrange
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());
        _adapterFactory.Setup(f => f.Resolve(It.IsAny<PlatformType>()))
            .Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformListQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(p => p.StoreCount == 0);
    }

    [Fact]
    public async Task Handle_WithStores_ShouldCountCorrectly()
    {
        // Arrange
        var stores = new List<Store>
        {
            FakeData.CreateStore(_tenantId, PlatformType.Trendyol),
            FakeData.CreateStore(_tenantId, PlatformType.Trendyol),
        }.AsReadOnly();

        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);
        _adapterFactory.Setup(f => f.Resolve(It.IsAny<PlatformType>()))
            .Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformListQuery(_tenantId), CancellationToken.None);

        // Assert
        var trendyolCard = result.FirstOrDefault(p => p.Platform == PlatformType.Trendyol);
        trendyolCard.Should().NotBeNull();
        trendyolCard!.StoreCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetPlatformSyncStatusHandler

[Trait("Category", "Unit")]
public class GetPlatformSyncStatusHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetPlatformSyncStatusHandler CreateHandler() =>
        new(_storeRepo.Object, _adapterFactory.Object);

    [Fact]
    public async Task Handle_NoStoresForAnyPlatform_ShouldReturnEmptyList()
    {
        // Arrange
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformSyncStatusQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithStores_ShouldReturnPlatformSyncStatus()
    {
        // Arrange
        var stores = new List<Store>
        {
            FakeData.CreateStore(_tenantId, PlatformType.Trendyol),
        }.AsReadOnly();

        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        var mockAdapter = new Mock<IIntegratorAdapter>();
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(mockAdapter.Object);
        _adapterFactory.Setup(f => f.Resolve(It.Is<PlatformType>(p => p != PlatformType.Trendyol)))
            .Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformSyncStatusQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].PlatformName.Should().Be("Trendyol");
        result[0].HealthStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_NoAdapterForPlatform_ShouldShowErrorStatus()
    {
        // Arrange
        var stores = new List<Store>
        {
            FakeData.CreateStore(_tenantId, PlatformType.Ozon),
        }.AsReadOnly();

        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);
        _adapterFactory.Setup(f => f.Resolve(It.IsAny<PlatformType>()))
            .Returns((IIntegratorAdapter?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformSyncStatusQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].HealthStatus.Should().Be("Error");
        result[0].HealthColor.Should().Be("#dc3545");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
