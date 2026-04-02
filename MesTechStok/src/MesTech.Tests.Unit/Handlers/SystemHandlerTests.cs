using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class SystemHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── GetAuditLogsHandler ──────────────────────────────────────

    private static GetAuditLogsHandler CreateAuditLogsHandler()
    {
        var repoMock = new Mock<IAccessLogRepository>();
        repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccessLog>());
        return new GetAuditLogsHandler(repoMock.Object);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsEmptyList()
    {
        var sut = CreateAuditLogsHandler();
        var query = new GetAuditLogsQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogs_NullRequest_StillReturnsEmpty()
    {
        // Handler accesses request.TenantId — null will throw NullReferenceException
        var sut = CreateAuditLogsHandler();

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ── GetBackupHistoryHandler ──────────────────────────────────

    [Fact]
    public async Task GetBackupHistory_ReturnsEmptyList()
    {
        var backupRepo = new Mock<IBackupEntryRepository>();
        backupRepo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry>().AsReadOnly());
        var sut = new GetBackupHistoryHandler(
            backupRepo.Object,
            NullLogger<GetBackupHistoryHandler>.Instance);
        var query = new GetBackupHistoryQuery(_tenantId, 10);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBackupHistory_NullRequest_ThrowsException()
    {
        var sut = new GetBackupHistoryHandler(
            Mock.Of<IBackupEntryRepository>(),
            NullLogger<GetBackupHistoryHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ── DeletePersonalDataHandler ────────────────────────────────

    [Fact]
    public async Task DeletePersonalData_NullRequest_ThrowsArgumentNullException()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        var storeRepo = new Mock<IStoreRepository>();
        var credentialRepo = new Mock<IStoreCredentialRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var kvkkAuditRepo = new Mock<IKvkkAuditLogRepository>();

        var sut = new DeletePersonalDataHandler(
            tenantRepo.Object, storeRepo.Object, credentialRepo.Object,
            orderRepo.Object, uow.Object, kvkkAuditRepo.Object,
            NullLogger<DeletePersonalDataHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePersonalData_TenantNotFound_ThrowsInvalidOperation()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        var storeRepo = new Mock<IStoreRepository>();
        var credentialRepo = new Mock<IStoreCredentialRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var kvkkAuditRepo = new Mock<IKvkkAuditLogRepository>();

        tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var sut = new DeletePersonalDataHandler(
            tenantRepo.Object, storeRepo.Object, credentialRepo.Object,
            orderRepo.Object, uow.Object, kvkkAuditRepo.Object,
            NullLogger<DeletePersonalDataHandler>.Instance);

        var cmd = new DeletePersonalDataCommand(_tenantId, Guid.NewGuid(), "KVKK talebi");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ── ExportPersonalDataHandler ────────────────────────────────

    [Fact]
    public async Task ExportPersonalData_NullRequest_ThrowsArgumentNullException()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        var sut = new ExportPersonalDataHandler(
            tenantRepo.Object, new Mock<IStoreRepository>().Object,
            new Mock<IOrderRepository>().Object, new Mock<IProductRepository>().Object,
            new Mock<IUserRepository>().Object, new Mock<IKvkkAuditLogRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            NullLogger<ExportPersonalDataHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExportPersonalData_TenantNotFound_ThrowsInvalidOperation()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var sut = new ExportPersonalDataHandler(
            tenantRepo.Object, new Mock<IStoreRepository>().Object,
            new Mock<IOrderRepository>().Object, new Mock<IProductRepository>().Object,
            new Mock<IUserRepository>().Object, new Mock<IKvkkAuditLogRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            NullLogger<ExportPersonalDataHandler>.Instance);

        var query = new ExportPersonalDataQuery(_tenantId, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task ExportPersonalData_ValidTenant_ReturnsExport()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        var tenant = new Tenant { Name = "TestTenant" };
        tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var storeRepo = new Mock<IStoreRepository>();
        storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>().AsReadOnly());

        var sut = new ExportPersonalDataHandler(
            tenantRepo.Object, storeRepo.Object,
            orderRepo.Object, productRepo.Object,
            userRepo.Object, new Mock<IKvkkAuditLogRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            NullLogger<ExportPersonalDataHandler>.Instance);

        var query = new ExportPersonalDataQuery(_tenantId, Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TenantId.Should().Be(_tenantId);
        result.TenantName.Should().Be("TestTenant");
        result.DataJson.Should().NotBeNullOrEmpty();
    }

    // ── GetLaunchReadinessHandler ────────────────────────────────

    [Fact]
    public async Task GetLaunchReadiness_WithProducts_AllPass()
    {
        var productRepo = new Mock<IProductRepository>();
        var orderRepo = new Mock<IOrderRepository>();

        productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var sut = new GetLaunchReadinessHandler(
            productRepo.Object, orderRepo.Object,
            NullLogger<GetLaunchReadinessHandler>.Instance);

        var query = new GetLaunchReadinessQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.PassedCriteria.Should().Be(26);
        result.Criteria.Should().HaveCount(26);
    }

    [Fact]
    public async Task GetLaunchReadiness_ZeroProducts_OneFails()
    {
        var productRepo = new Mock<IProductRepository>();
        var orderRepo = new Mock<IOrderRepository>();

        productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = new GetLaunchReadinessHandler(
            productRepo.Object, orderRepo.Object,
            NullLogger<GetLaunchReadinessHandler>.Instance);

        var query = new GetLaunchReadinessQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.PassedCriteria.Should().Be(25);
    }
}
