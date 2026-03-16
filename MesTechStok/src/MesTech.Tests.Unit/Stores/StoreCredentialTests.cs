using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using MesTech.Application.Features.Stores.Commands.TestStoreCredential;
using MesTech.Application.Features.Stores.Queries.GetStoreCredential;
using MesTech.Application.Interfaces;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Stores;

/// <summary>
/// ENT-DALGA12-002: Store Credential Registration Flow tests.
/// Covers: save, test, get (masked), delete, tenant isolation, timeout.
/// </summary>
public class StoreCredentialTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _storeId = Guid.NewGuid();

    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICredentialEncryptionService> _encryptionMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();

    private Store CreateTestStore(Guid? tenantId = null, Guid? storeId = null)
    {
        var store = new Store
        {
            TenantId = tenantId ?? _tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store",
            IsActive = true
        };
        // Set Id via reflection since it's protected setter
        typeof(MesTech.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(store, storeId ?? _storeId);
        return store;
    }

    // ──────────────────────────────────────────────
    // 1. SaveCredential_ValidInput_Succeeds
    // ──────────────────────────────────────────────
    [Fact]
    public async Task SaveCredential_ValidInput_Succeeds()
    {
        // Arrange
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());
        _encryptionMock.Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns<string>(s => $"ENC:{s}");
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SaveStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _uowMock.Object, _encryptionMock.Object,
            Mock.Of<ILogger<SaveStoreCredentialHandler>>());

        var command = new SaveStoreCredentialCommand
        {
            StoreId = _storeId,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>
            {
                { "ApiKey", "test-key-12345" },
                { "Secret", "test-secret-67890" }
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _credentialRepoMock.Verify(r => r.AddAsync(It.IsAny<StoreCredential>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────
    // 2. SaveCredential_EmptyStoreId_Rejected (validator)
    // ──────────────────────────────────────────────
    [Fact]
    public void SaveCredential_EmptyStoreId_Rejected()
    {
        var validator = new SaveStoreCredentialValidator();
        var command = new SaveStoreCredentialCommand
        {
            StoreId = Guid.Empty,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string> { { "ApiKey", "val" } }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    // ──────────────────────────────────────────────
    // 3. SaveCredential_EmptyFields_Rejected
    // ──────────────────────────────────────────────
    [Fact]
    public void SaveCredential_EmptyFields_Rejected()
    {
        var validator = new SaveStoreCredentialValidator();
        var command = new SaveStoreCredentialCommand
        {
            StoreId = _storeId,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string>()
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fields");
    }

    // ──────────────────────────────────────────────
    // 4. TestCredential_ValidStore_ReturnsPingResult
    // ──────────────────────────────────────────────
    [Fact]
    public async Task TestCredential_ValidStore_ReturnsPingResult()
    {
        // Arrange
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var credentials = new List<StoreCredential>
        {
            new()
            {
                TenantId = _tenantId,
                StoreId = _storeId,
                Key = "api_key:ApiKey",
                EncryptedValue = "ENC:test-key"
            }
        };
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _encryptionMock.Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns<string>(s => s.Replace("ENC:", ""));

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PlatformCode).Returns("Trendyol");
        adapterMock.Setup(a => a.TestConnectionAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto
            {
                IsSuccess = true,
                PlatformCode = "Trendyol",
                ProductCount = 42,
                ResponseTime = TimeSpan.FromMilliseconds(150)
            });
        _adapterFactoryMock.Setup(f => f.Resolve("Trendyol")).Returns(adapterMock.Object);

        var handler = new TestStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object, _adapterFactoryMock.Object,
            Mock.Of<ILogger<TestStoreCredentialHandler>>());

        // Act
        var result = await handler.Handle(new TestStoreCredentialCommand(_storeId), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Platform.Should().Be("Trendyol");
        result.LatencyMs.Should().BeGreaterOrEqualTo(0);
        result.Message.Should().Contain("42");
    }

    // ──────────────────────────────────────────────
    // 5. TestCredential_InvalidStore_ReturnsFailure
    // ──────────────────────────────────────────────
    [Fact]
    public async Task TestCredential_InvalidStore_ReturnsFailure()
    {
        _storeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var handler = new TestStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object, _adapterFactoryMock.Object,
            Mock.Of<ILogger<TestStoreCredentialHandler>>());

        var result = await handler.Handle(
            new TestStoreCredentialCommand(Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    // ──────────────────────────────────────────────
    // 6. GetCredential_ReturnsMaskedValues
    // ──────────────────────────────────────────────
    [Fact]
    public async Task GetCredential_ReturnsMaskedValues()
    {
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var credentials = new List<StoreCredential>
        {
            new()
            {
                TenantId = _tenantId,
                StoreId = _storeId,
                Key = "api_key:ApiKey",
                EncryptedValue = "ENC:trendyol-api-key-12345",
                UpdatedAt = DateTime.UtcNow
            }
        };
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _encryptionMock.Setup(e => e.Decrypt("ENC:trendyol-api-key-12345"))
            .Returns("trendyol-api-key-12345");
        _encryptionMock.Setup(e => e.Mask("trendyol-api-key-12345"))
            .Returns("tren****2345");

        var handler = new GetStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object,
            Mock.Of<ILogger<GetStoreCredentialHandler>>());

        var result = await handler.Handle(
            new GetStoreCredentialQuery(_storeId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.MaskedFields.Should().ContainKey("ApiKey");
        result.MaskedFields["ApiKey"].Should().Be("tren****2345");
        result.Platform.Should().Be("Trendyol");
    }

    // ──────────────────────────────────────────────
    // 7. GetCredential_NeverReturnsPlaintext
    // ──────────────────────────────────────────────
    [Fact]
    public async Task GetCredential_NeverReturnsPlaintext()
    {
        var plainSecret = "super-secret-api-key-value";
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var credentials = new List<StoreCredential>
        {
            new()
            {
                TenantId = _tenantId,
                StoreId = _storeId,
                Key = "api_key:Secret",
                EncryptedValue = "ENC:encrypted-blob",
                UpdatedAt = DateTime.UtcNow
            }
        };
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _encryptionMock.Setup(e => e.Decrypt("ENC:encrypted-blob"))
            .Returns(plainSecret);
        _encryptionMock.Setup(e => e.Mask(plainSecret))
            .Returns("supe****alue");

        var handler = new GetStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object,
            Mock.Of<ILogger<GetStoreCredentialHandler>>());

        var result = await handler.Handle(
            new GetStoreCredentialQuery(_storeId), CancellationToken.None);

        result.Should().NotBeNull();
        // The returned DTO must NOT contain the plaintext value
        var allValues = result!.MaskedFields.Values;
        allValues.Should().NotContain(plainSecret);
        allValues.Should().OnlyContain(v => v.Contains("****"));
    }

    // ──────────────────────────────────────────────
    // 8. DeleteCredential_ExistingStore_SoftDeletes
    // ──────────────────────────────────────────────
    [Fact]
    public async Task DeleteCredential_ExistingStore_SoftDeletes()
    {
        var credentials = new List<StoreCredential>
        {
            new() { TenantId = _tenantId, StoreId = _storeId, Key = "api_key:ApiKey", EncryptedValue = "ENC:val1" },
            new() { TenantId = _tenantId, StoreId = _storeId, Key = "api_key:Secret", EncryptedValue = "ENC:val2" }
        };
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = new DeleteStoreCredentialHandler(
            _credentialRepoMock.Object, _uowMock.Object,
            Mock.Of<ILogger<DeleteStoreCredentialHandler>>());

        var result = await handler.Handle(
            new DeleteStoreCredentialCommand(_storeId), CancellationToken.None);

        result.Should().BeTrue();
        credentials.Should().OnlyContain(c => c.IsDeleted);
        credentials.Should().OnlyContain(c => c.DeletedAt != null);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────
    // 9. SaveCredential_TenantIsolation
    // ──────────────────────────────────────────────
    [Fact]
    public async Task SaveCredential_TenantIsolation()
    {
        var otherTenantId = Guid.NewGuid();
        var store = CreateTestStore(_tenantId, _storeId);
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var handler = new SaveStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _uowMock.Object, _encryptionMock.Object,
            Mock.Of<ILogger<SaveStoreCredentialHandler>>());

        var command = new SaveStoreCredentialCommand
        {
            StoreId = _storeId,
            TenantId = otherTenantId, // Different tenant!
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string> { { "ApiKey", "val" } }
        };

        // Act & Assert: should throw because tenant mismatch
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    // 10. TestCredential_Timeout_GracefulFailure
    // ──────────────────────────────────────────────
    [Fact]
    public async Task TestCredential_Timeout_GracefulFailure()
    {
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var credentials = new List<StoreCredential>
        {
            new()
            {
                TenantId = _tenantId,
                StoreId = _storeId,
                Key = "api_key:ApiKey",
                EncryptedValue = "ENC:key"
            }
        };
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _encryptionMock.Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns("decrypted");

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PlatformCode).Returns("Trendyol");
        adapterMock.Setup(a => a.TestConnectionAsync(
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Timeout"));
        _adapterFactoryMock.Setup(f => f.Resolve("Trendyol")).Returns(adapterMock.Object);

        var handler = new TestStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object, _adapterFactoryMock.Object,
            Mock.Of<ILogger<TestStoreCredentialHandler>>());

        var result = await handler.Handle(
            new TestStoreCredentialCommand(_storeId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timed out");
        result.Platform.Should().Be("Trendyol");
    }

    // ──────────────────────────────────────────────
    // 11. SaveCredential_InvalidCredentialType_Rejected (validator)
    // ──────────────────────────────────────────────
    [Fact]
    public void SaveCredential_InvalidCredentialType_Rejected()
    {
        var validator = new SaveStoreCredentialValidator();
        var command = new SaveStoreCredentialCommand
        {
            StoreId = _storeId,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "invalid_type",
            Fields = new Dictionary<string, string> { { "ApiKey", "val" } }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CredentialType");
    }

    // ──────────────────────────────────────────────
    // 12. DeleteCredential_NoExisting_ReturnsFalse
    // ──────────────────────────────────────────────
    [Fact]
    public async Task DeleteCredential_NoExisting_ReturnsFalse()
    {
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        var handler = new DeleteStoreCredentialHandler(
            _credentialRepoMock.Object, _uowMock.Object,
            Mock.Of<ILogger<DeleteStoreCredentialHandler>>());

        var result = await handler.Handle(
            new DeleteStoreCredentialCommand(_storeId), CancellationToken.None);

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────
    // 13. CredentialEncryption_Mask_ShortValue
    // ──────────────────────────────────────────────
    [Fact]
    public void CredentialEncryption_Mask_ShortValue()
    {
        var service = new MesTech.Infrastructure.Security.CredentialEncryptionService(
            Mock.Of<MesTech.Infrastructure.Security.IFieldEncryptionService>(),
            Mock.Of<ILogger<MesTech.Infrastructure.Security.CredentialEncryptionService>>());

        service.Mask("ab").Should().Be("****");
        service.Mask("abcde").Should().Be("a****e");
        service.Mask("abcdefghij").Should().Be("abcd****ghij");
        service.Mask("").Should().Be("****");
    }

    // ──────────────────────────────────────────────
    // 14. TestCredential_NoCredentialsSaved_ReturnsFailure
    // ──────────────────────────────────────────────
    [Fact]
    public async Task TestCredential_NoCredentialsSaved_ReturnsFailure()
    {
        var store = CreateTestStore();
        _storeRepoMock.Setup(r => r.GetByIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);
        _credentialRepoMock.Setup(r => r.GetByStoreIdAsync(_storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());

        // Set up adapter so handler proceeds past adapter check to credential check
        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PlatformCode).Returns("Trendyol");
        _adapterFactoryMock.Setup(f => f.Resolve("Trendyol")).Returns(adapterMock.Object);

        var handler = new TestStoreCredentialHandler(
            _storeRepoMock.Object, _credentialRepoMock.Object,
            _encryptionMock.Object, _adapterFactoryMock.Object,
            Mock.Of<ILogger<TestStoreCredentialHandler>>());

        var result = await handler.Handle(
            new TestStoreCredentialCommand(_storeId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No credentials");
    }
}
