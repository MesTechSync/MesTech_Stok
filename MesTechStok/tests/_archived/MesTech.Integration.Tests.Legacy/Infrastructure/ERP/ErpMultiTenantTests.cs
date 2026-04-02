using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.ERP;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// Multi-tenant ERP isolation tests — validates that ERP adapters, sync logs,
/// and conflict logs correctly isolate data per tenant.
/// I-14 ERP Saglamlastirma / T-04 Multi-Tenant Tests.
/// </summary>
[Trait("Category", "Integration")]
public class ErpMultiTenantTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly Guid TenantC = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════════════
    // Adapter Factory Tenant Isolation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void TenantA_Logo_TenantB_Netsis_BothResolve()
    {
        // Arrange — simulate two tenants using different ERP providers
        var logoAdapter = new Mock<IErpAdapter>();
        logoAdapter.Setup(a => a.Provider).Returns(ErpProvider.Logo);

        var netsisAdapter = new Mock<IErpAdapter>();
        netsisAdapter.Setup(a => a.Provider).Returns(ErpProvider.Netsis);

        var factory = new ERPAdapterFactory(
            new List<Application.Interfaces.Accounting.IERPAdapter>(),
            new List<IErpAdapter> { logoAdapter.Object, netsisAdapter.Object });

        // Act — TenantA uses Logo, TenantB uses Netsis
        var adapterForA = factory.GetAdapter(ErpProvider.Logo);
        var adapterForB = factory.GetAdapter(ErpProvider.Netsis);

        // Assert
        adapterForA.Should().NotBeNull();
        adapterForA.Provider.Should().Be(ErpProvider.Logo, "TenantA should resolve Logo adapter");

        adapterForB.Should().NotBeNull();
        adapterForB.Provider.Should().Be(ErpProvider.Netsis, "TenantB should resolve Netsis adapter");

        adapterForA.Should().NotBeSameAs(adapterForB,
            "different providers must return different adapter instances");
    }

    [Fact]
    public void TenantC_NoErp_GracefulHandling()
    {
        // Arrange — factory with only Logo registered
        var logoAdapter = new Mock<IErpAdapter>();
        logoAdapter.Setup(a => a.Provider).Returns(ErpProvider.Logo);

        var factory = new ERPAdapterFactory(
            new List<Application.Interfaces.Accounting.IERPAdapter>(),
            new List<IErpAdapter> { logoAdapter.Object });

        // Act — TenantC requests Mikro which is not registered
        var act = () => factory.GetAdapter(ErpProvider.Mikro);

        // Assert — should throw ArgumentException with meaningful message
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Mikro*");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ErpSyncLog Tenant Isolation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SyncLog_TenantIsolation()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        // Act — create sync logs for two different tenants
        var logA = ErpSyncLog.Create(TenantA, ErpProvider.Logo, "Order", entityId1);
        var logB = ErpSyncLog.Create(TenantB, ErpProvider.Netsis, "Invoice", entityId2);

        // Assert — tenant isolation
        logA.TenantId.Should().Be(TenantA);
        logB.TenantId.Should().Be(TenantB);
        logA.TenantId.Should().NotBe(logB.TenantId, "sync logs must belong to different tenants");

        // Provider isolation
        logA.Provider.Should().Be(ErpProvider.Logo);
        logB.Provider.Should().Be(ErpProvider.Netsis);

        // Entity isolation
        logA.EntityType.Should().Be("Order");
        logB.EntityType.Should().Be("Invoice");
        logA.EntityId.Should().Be(entityId1);
        logB.EntityId.Should().Be(entityId2);

        // Mark success on one tenant should not affect the other
        logA.MarkSuccess("LOGO-REF-001", 200);
        logA.Success.Should().BeTrue();
        logB.Success.Should().BeFalse("marking TenantA success must not affect TenantB");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ErpConflictLog Tenant Isolation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ConflictLog_TenantIsolation()
    {
        // Act — create conflict logs for two different tenants
        var conflictA = ErpConflictLog.Create(
            TenantA,
            ErpProvider.Logo,
            entityType: "Stock",
            entityCode: "SKU-001",
            mestechValue: "100",
            erpValue: "95",
            winner: "MesTech",
            resolution: "Auto");

        var conflictB = ErpConflictLog.Create(
            TenantB,
            ErpProvider.Parasut,
            entityType: "Price",
            entityCode: "SKU-002",
            mestechValue: "49.90",
            erpValue: "52.00",
            winner: "Erp",
            resolution: "Manual");

        // Assert — tenant isolation
        conflictA.TenantId.Should().Be(TenantA);
        conflictB.TenantId.Should().Be(TenantB);
        conflictA.TenantId.Should().NotBe(conflictB.TenantId,
            "conflict logs must belong to different tenants");

        // Provider isolation
        conflictA.Provider.Should().Be(ErpProvider.Logo);
        conflictB.Provider.Should().Be(ErpProvider.Parasut);

        // Data isolation
        conflictA.EntityCode.Should().Be("SKU-001");
        conflictB.EntityCode.Should().Be("SKU-002");

        conflictA.Winner.Should().Be("MesTech");
        conflictB.Winner.Should().Be("Erp");

        conflictA.Resolution.Should().Be("Auto");
        conflictB.Resolution.Should().Be("Manual");
    }
}
