using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// TestStoreConnectionHandler unit testleri.
/// Adapter baglanti testi basari ve basarisizlik senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class TestStoreConnectionHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepoMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly Mock<ILogger<TestStoreConnectionHandler>> _loggerMock = new();
    private readonly TestStoreConnectionHandler _handler;

    public TestStoreConnectionHandlerTests()
    {
        _handler = new TestStoreConnectionHandler(
            _storeRepoMock.Object,
            _credentialRepoMock.Object,
            _encryptionMock.Object,
            _adapterFactoryMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Basarili baglanti testi
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_Success_ReturnsTrue()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var store = new Store
        {
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store",
            IsActive = true
        };
        // Set Id via reflection since BaseEntity may set it
        typeof(Store).BaseType!.GetProperty("Id")!.SetValue(store, storeId);

        _storeRepoMock
            .Setup(r => r.GetByIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var credentials = new List<StoreCredential>
        {
            new() { Key = "ApiKey", EncryptedValue = "enc-key" },
            new() { Key = "ApiSecret", EncryptedValue = "enc-secret" },
        };

        _credentialRepoMock
            .Setup(r => r.GetByStoreIdAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        _encryptionMock
            .Setup(e => e.Decrypt("enc-key")).Returns("real-key");
        _encryptionMock
            .Setup(e => e.Decrypt("enc-secret")).Returns("real-secret");

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock
            .Setup(a => a.TestConnectionAsync(
                It.Is<Dictionary<string, string>>(d =>
                    d["ApiKey"] == "real-key" && d["ApiSecret"] == "real-secret"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto
            {
                IsSuccess = true,
                PlatformCode = "Trendyol",
                ProductCount = 42
            });

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        // Act
        var result = await _handler.Handle(
            new TestStoreConnectionCommand(storeId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Trendyol");
        result.ProductCount.Should().Be(42);
    }
}
