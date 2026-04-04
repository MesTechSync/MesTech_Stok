using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: TestStoreConnectionHandler testi — mağaza bağlantı testi.
/// P1: Bağlantı testi başarısız = platform entegrasyonu çalışmaz.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class TestStoreConnectionHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreCredentialRepository> _credRepo = new();
    private readonly Mock<ICredentialEncryptionService> _encryption = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ILogger<TestStoreConnectionHandler>> _logger = new();

    private TestStoreConnectionHandler CreateSut() =>
        new(_storeRepo.Object, _credRepo.Object, _encryption.Object, _adapterFactory.Object, _logger.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ShouldReturnFailure()
    {
        _storeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var cmd = new TestStoreConnectionCommand(Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoAdapterForPlatform_ShouldReturnFailure()
    {
        var store = new Store { PlatformType = PlatformType.Trendyol, TenantId = Guid.NewGuid() };
        _storeRepo.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>())).ReturnsAsync(store);
        _credRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns((IIntegratorAdapter?)null);

        var cmd = new TestStoreConnectionCommand(store.Id);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No adapter");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnSuccess()
    {
        var store = new Store { PlatformType = PlatformType.Hepsiburada, TenantId = Guid.NewGuid() };
        _storeRepo.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var cred = new StoreCredential { Key = "ApiKey", EncryptedValue = "enc_val" };
        _credRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { cred });
        _encryption.Setup(e => e.Decrypt("enc_val")).Returns("real_api_key");

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = true, ProductCount = 500 });
        _adapterFactory.Setup(f => f.Resolve(PlatformType.Hepsiburada)).Returns(adapter.Object);

        var cmd = new TestStoreConnectionCommand(store.Id);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ProductCount.Should().Be(500);
    }

    [Fact]
    public async Task Handle_AdapterThrows_ShouldReturnErrorGracefully()
    {
        var store = new Store { PlatformType = PlatformType.N11, TenantId = Guid.NewGuid() };
        _storeRepo.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>())).ReturnsAsync(store);
        _credRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("API timeout"));
        _adapterFactory.Setup(f => f.Resolve(PlatformType.N11)).Returns(adapter.Object);

        var cmd = new TestStoreConnectionCommand(store.Id);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timeout");
    }
}
