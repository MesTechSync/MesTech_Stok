using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

/// <summary>
/// DeletePersonalDataHandler: KVKK Madde 7 — kişisel veri anonimizasyonu.
/// YASAL UYUM KRİTİK — hukuki risk taşıyor.
/// Kritik iş kuralları:
///   - Tenant bilgileri "ANONİM" olmalı, TaxNumber null
///   - Kullanıcı isimleri anonimleştirilmeli, e-posta hashlenmiş pseudonym
///   - Store credential'ları tamamen silinmeli (API key/secret)
///   - Sipariş müşteri bilgileri anonimleştirilmeli
///   - Toplam anonymized kayıt sayısı doğru dönmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "KvkkCompliance")]
public class DeletePersonalDataHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<DeletePersonalDataHandler>> _logger = new();
    public DeletePersonalDataHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tenantRepo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _credentialRepo.Setup(r => r.DeleteAsync(It.IsAny<StoreCredential>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private DeletePersonalDataHandler CreateHandler() =>
        new(_tenantRepo.Object, _storeRepo.Object, _credentialRepo.Object,
            _orderRepo.Object, _uow.Object, _logger.Object);

    private Tenant CreateTenantWithUsers(int userCount)
    {
        var tenant = new Tenant
        {
            Name = "Test Firma A.Ş.",
            TaxNumber = "1234567890"
        };
        for (int i = 0; i < userCount; i++)
        {
            tenant.Users.Add(new User
            {
                FirstName = $"Kullanıcı{i}",
                LastName = $"Soyad{i}",
                Email = $"user{i}@test.com",
                Phone = $"0555000000{i}",
                IsActive = true
            });
        }
        return tenant;
    }

    [Fact]
    public async Task Handle_AnonymizesTenantInfo()
    {
        // Arrange
        var tenant = CreateTenantWithUsers(0);
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "KVKK Madde 7 talebi");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — tenant bilgileri anonimleştirilmiş olmalı
        result.Success.Should().BeTrue();
        tenant.Name.Should().Be("ANONIM");
        tenant.TaxNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AnonymizesUserPersonalData()
    {
        // Arrange — 3 kullanıcılı tenant
        var tenant = CreateTenantWithUsers(3);
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Kullanıcı talebi");
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — tüm kullanıcılar anonimleştirilmiş olmalı
        foreach (var user in tenant.Users)
        {
            user.FirstName.Should().Be("ANONIM");
            user.LastName.Should().BeNull();
            user.Email.Should().EndWith("@anon.mestech.app");
            user.Phone.Should().BeNull();
            user.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Handle_DeletesStoreCredentials()
    {
        // Arrange — 1 store, 2 credential
        var tenant = CreateTenantWithUsers(0);
        var store = new Store { TenantId = Guid.NewGuid(), StoreName = "Test Store" };
        var creds = new List<StoreCredential>
        {
            new() { StoreId = store.Id, Key = "ApiKey", EncryptedValue = "ENC:xxx" },
            new() { StoreId = store.Id, Key = "ApiSecret", EncryptedValue = "ENC:yyy" }
        };

        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store> { store });
        _credentialRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creds);
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Credential silme");
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — 2 credential silinmiş olmalı
        _credentialRepo.Verify(r => r.DeleteAsync(It.IsAny<StoreCredential>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_AnonymizesOrderCustomerInfo()
    {
        // Arrange — 2 sipariş
        var tenant = CreateTenantWithUsers(0);
        var orders = new List<Order>
        {
            new() { CustomerName = "Ahmet Yılmaz", CustomerEmail = "ahmet@test.com" },
            new() { CustomerName = "Mehmet Kaya", CustomerEmail = "mehmet@test.com" }
        };

        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Sipariş anonimizasyonu");
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — sipariş müşteri bilgileri anonimleştirilmiş
        foreach (var order in orders)
        {
            order.CustomerName.Should().Be("ANONIM");
            order.CustomerEmail.Should().BeNull();
        }
    }

    [Fact]
    public async Task Handle_ReturnsCorrectAnonymizedCount()
    {
        // Arrange — 1 tenant + 2 user + 1 credential + 3 sipariş = 7 kayıt
        var tenant = CreateTenantWithUsers(2);
        var store = new Store { TenantId = Guid.NewGuid(), StoreName = "S" };
        var cred = new StoreCredential { StoreId = store.Id, Key = "K", EncryptedValue = "V" };
        var orders = new List<Order>
        {
            new() { CustomerName = "A", CustomerEmail = "a@t.com" },
            new() { CustomerName = "B", CustomerEmail = "b@t.com" },
            new() { CustomerName = "C", CustomerEmail = "c@t.com" }
        };

        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store> { store });
        _credentialRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<StoreCredential> { cred });
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Tam anonimizasyon");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — 1(tenant) + 2(user) + 1(cred) + 3(order) = 7
        result.AnonymizedRecords.Should().Be(7);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ThrowsInvalidOperation()
    {
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Bulunamayan tenant");
        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SaveChangesCalledOnce_AtomicOperation()
    {
        // Arrange — tüm anonimizasyon tek SaveChanges ile persist edilmeli
        var tenant = CreateTenantWithUsers(1);
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Atomik test");
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        // Assert — tek atomik transaction
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
