using FluentAssertions;
using MesTech.Application.Features.Stores.Queries.GetStoreCredential;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Stores.Queries;

[Trait("Category", "Unit")]
public class GetStoreCredentialHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credRepoMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionMock = new();
    private readonly Mock<ILogger<GetStoreCredentialHandler>> _loggerMock = new();
    private readonly GetStoreCredentialHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Store _testStore;

    public GetStoreCredentialHandlerTests()
    {
        _sut = new GetStoreCredentialHandler(
            _storeRepoMock.Object, _credRepoMock.Object,
            _encryptionMock.Object, _loggerMock.Object);
        _testStore = new Store
        {
            TenantId = TenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store"
        };
    }

    [Fact]
    public async Task Handle_StoreAndCredentialsExist_ShouldReturnMaskedDto()
    {
        // Arrange
        var storeId = _testStore.Id;
        var cred = new StoreCredential
        {
            TenantId = TenantId, StoreId = storeId,
            Key = "api_key:ApiToken", EncryptedValue = "enc_value",
            UpdatedAt = new DateTime(2026, 3, 20)
        };
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { cred }.AsReadOnly());
        _encryptionMock.Setup(e => e.Decrypt("enc_value")).Returns("my-secret-api-token");
        _encryptionMock.Setup(e => e.Mask("my-secret-api-token")).Returns("my-s***oken");

        // Act
        var result = await _sut.Handle(new GetStoreCredentialQuery(storeId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.StoreId.Should().Be(storeId);
        result.Platform.Should().Be("Trendyol");
        result.CredentialType.Should().Be("api_key");
        result.MaskedFields.Should().ContainKey("ApiToken");
        result.MaskedFields["ApiToken"].Should().Be("my-s***oken");
    }

    [Fact]
    public async Task Handle_StoreNotFound_ShouldReturnNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _storeRepoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _sut.Handle(new GetStoreCredentialQuery(missingId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StoreExistsButNoCredentials_ShouldReturnNull()
    {
        // Arrange
        var storeId = _testStore.Id;
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>().AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetStoreCredentialQuery(storeId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_CredentialKeyWithoutColon_ShouldUseFullKeyAsFieldName()
    {
        // Arrange
        var storeId = _testStore.Id;
        var cred = new StoreCredential
        {
            TenantId = TenantId, StoreId = storeId,
            Key = "SimpleKey", EncryptedValue = "enc",
            UpdatedAt = DateTime.UtcNow
        };
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { cred }.AsReadOnly());
        _encryptionMock.Setup(e => e.Decrypt("enc")).Returns("value");
        _encryptionMock.Setup(e => e.Mask("value")).Returns("****");

        // Act
        var result = await _sut.Handle(new GetStoreCredentialQuery(storeId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.MaskedFields.Should().ContainKey("SimpleKey");
    }
}
