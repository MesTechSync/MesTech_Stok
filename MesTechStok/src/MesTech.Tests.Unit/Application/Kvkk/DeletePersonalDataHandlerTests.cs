using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Kvkk;

// ════════════════════════════════════════════════════════
// DEV5 TUR 14: KVKK DeletePersonalData handler tests (G230)
// KVKK Madde 7: Kisisel verilerin silinmesi/anonimlestirilmesi
// Yasal uyum kritik — test olmadan uyum iddiasi yapilamaz
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Security", "KVKK")]
public class DeletePersonalDataHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreCredentialRepository> _credentialRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IKvkkAuditLogRepository> _kvkkAuditRepo = new();
    private readonly Mock<ILogger<DeletePersonalDataHandler>> _logger = new();

    private DeletePersonalDataHandler CreateSut() =>
        new(_tenantRepo.Object, _storeRepo.Object, _credentialRepo.Object,
            _orderRepo.Object, _uow.Object, _kvkkAuditRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_TenantNotFound_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut();
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "Test");

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant bulunamadi*");
    }

    [Fact]
    public async Task Handle_ValidTenant_ShouldAnonymizeTenantName()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Name = "Acme Corp", TaxNumber = "1234567890" };
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);

        _tenantRepo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var result = await sut.Handle(
            new DeletePersonalDataCommand(tenantId, Guid.NewGuid(), "KVKK talebi"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AnonymizedRecords.Should().BeGreaterThanOrEqualTo(1);
        tenant.Name.Should().Be("ANONIM");
        tenant.TaxNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldDeleteStoreCredentials()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Name = "Test Corp" };
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);

        var store = new Store { TenantId = tenantId };
        var credential = new StoreCredential { StoreId = store.Id };

        _tenantRepo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store });
        _credentialRepo.Setup(r => r.GetByStoreIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { credential });
        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        await sut.Handle(
            new DeletePersonalDataCommand(tenantId, Guid.NewGuid(), "Credential temizlik"), CancellationToken.None);

        _credentialRepo.Verify(r => r.DeleteAsync(credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateKvkkAuditLog()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Name = "Audit Corp" };
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);

        _tenantRepo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        await sut.Handle(
            new DeletePersonalDataCommand(tenantId, userId, "Yasal talep"), CancellationToken.None);

        _kvkkAuditRepo.Verify(r => r.AddAsync(
            It.Is<KvkkAuditLog>(log => log.TenantId == tenantId && log.IsSuccess),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTimestamp()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Name = "Time Corp" };
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);

        _tenantRepo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var before = DateTime.UtcNow;
        var result = await sut.Handle(
            new DeletePersonalDataCommand(tenantId, Guid.NewGuid(), "Test"), CancellationToken.None);

        result.ProcessedAt.Should().BeOnOrAfter(before);
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
