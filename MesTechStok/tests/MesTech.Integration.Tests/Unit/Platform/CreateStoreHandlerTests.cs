using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// CreateStoreHandler unit testleri.
/// Credential encryption ve connection test basarisizlik senaryolarini test eder.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class CreateStoreHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepoMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CreateStoreHandler>> _loggerMock = new();
    private readonly CreateStoreHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateStoreHandlerTests()
    {
        _handler = new CreateStoreHandler(
            _storeRepoMock.Object,
            _credentialRepoMock.Object,
            _encryptionMock.Object,
            _adapterFactoryMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Credential sifrelenmis olarak kaydedilmeli
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateStore_SavesCredentialEncrypted()
    {
        // Arrange
        var credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "my-secret-key",
            ["ApiSecret"] = "my-secret-secret"
        };

        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string plain) => $"ENC:{plain}");

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock
            .Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = true, PlatformCode = "Trendyol" });

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        var command = new CreateStoreCommand(
            _tenantId, "Test Store", PlatformType.Trendyol, credentials);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoreId.Should().NotBeNull();

        _encryptionMock.Verify(
            e => e.Encrypt("my-secret-key"), Times.Once);
        _encryptionMock.Verify(
            e => e.Encrypt("my-secret-secret"), Times.Once);

        _credentialRepoMock.Verify(
            r => r.AddAsync(
                It.Is<StoreCredential>(c => c.EncryptedValue == "ENC:my-secret-key"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Baglanti testi basarisiz olursa hata donmeli ve rollback
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateStore_TestConnectionFails_ReturnsError()
    {
        // Arrange
        var credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "bad-key"
        };

        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns("encrypted");

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock
            .Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Authentication failed"
            });

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        var command = new CreateStoreCommand(
            _tenantId, "Bad Store", PlatformType.Trendyol, credentials);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection test failed");
        result.StoreId.Should().BeNull();

        // Store should have been rolled back (deleted)
        _storeRepoMock.Verify(
            r => r.DeleteAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
