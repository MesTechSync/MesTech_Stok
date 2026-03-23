using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Stores.Commands;

[Trait("Category", "Unit")]
public class SaveStoreCredentialHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionMock = new();
    private readonly Mock<ILogger<SaveStoreCredentialHandler>> _loggerMock = new();
    private readonly SaveStoreCredentialHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Store _testStore;

    public SaveStoreCredentialHandlerTests()
    {
        _sut = new SaveStoreCredentialHandler(
            _storeRepoMock.Object, _credRepoMock.Object,
            _uowMock.Object, _encryptionMock.Object, _loggerMock.Object);
        _testStore = new Store
        {
            TenantId = TenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store"
        };
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldEncryptAndSaveCredentials()
    {
        // Arrange
        var storeId = _testStore.Id;
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>().AsReadOnly());
        _encryptionMock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_value");

        var command = new SaveStoreCredentialCommand
        {
            StoreId = storeId,
            TenantId = TenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ApiKey", "test-api-key" },
                { "Secret", "test-secret" }
            }
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _credRepoMock.Verify(r => r.AddAsync(It.IsAny<StoreCredential>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _encryptionMock.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_StoreNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missingStoreId = Guid.NewGuid();
        _storeRepoMock.Setup(r => r.GetByIdAsync(missingStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var command = new SaveStoreCredentialCommand
        {
            StoreId = missingStoreId,
            TenantId = TenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>(StringComparer.Ordinal) { { "ApiKey", "val" } }
        };

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{missingStoreId}*not found*");
    }

    [Fact]
    public async Task Handle_StoreBelongsToDifferentTenant_ShouldThrowUnauthorized()
    {
        // Arrange
        var storeId = _testStore.Id;
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);

        var otherTenantId = Guid.NewGuid();
        var command = new SaveStoreCredentialCommand
        {
            StoreId = storeId,
            TenantId = otherTenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>(StringComparer.Ordinal) { { "Key", "val" } }
        };

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
    public async Task Handle_ExistingCredentials_ShouldSoftDeleteOldOnesFirst()
    {
        // Arrange
        var storeId = _testStore.Id;
        var existingCred = new StoreCredential
        {
            TenantId = TenantId, StoreId = storeId,
            Key = "api_key:OldKey", EncryptedValue = "old_enc"
        };
        _storeRepoMock.Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testStore);
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { existingCred }.AsReadOnly());
        _encryptionMock.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("new_enc");

        var command = new SaveStoreCredentialCommand
        {
            StoreId = storeId,
            TenantId = TenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>(StringComparer.Ordinal) { { "NewKey", "new-val" } }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        existingCred.IsDeleted.Should().BeTrue();
        existingCred.DeletedAt.Should().NotBeNull();
        _credRepoMock.Verify(r => r.UpdateAsync(existingCred, It.IsAny<CancellationToken>()), Times.Once);
    }
}
