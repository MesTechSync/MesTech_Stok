using FluentAssertions;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Erp;

/// <summary>
/// DEV 5 — Dalga 11 Task 5.1: ErpSyncLog entity tests.
/// Tests CreateSuccess, CreateFailure, MarkRetried, EntityType preservation.
/// Depends on: DEV 1 Task 1.1 (ErpProvider enum) + Task 1.2 (ErpSyncLog entity).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ERP")]
[Trait("Phase", "Dalga11")]
public class ErpSyncLogTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _entityId = Guid.NewGuid();

    [Fact]
    public void CreateSuccess_ShouldSetIsSuccessTrue()
    {
        var log = ErpSyncLog.CreateSuccess(
            _tenantId, ErpProvider.Logo, "Order", _entityId, "LOGO-REF-001");

        log.IsSuccess.Should().BeTrue();
        log.ErpReferenceNumber.Should().Be("LOGO-REF-001");
        log.Provider.Should().Be(ErpProvider.Logo);
        log.AttemptCount.Should().Be(1);
        log.TenantId.Should().Be(_tenantId);
        log.EntityId.Should().Be(_entityId);
    }

    [Fact]
    public void CreateFailure_ShouldSetIsSuccessFalse()
    {
        var log = ErpSyncLog.CreateFailure(
            _tenantId, ErpProvider.Logo, "Order", _entityId, "Bağlantı hatası");

        log.IsSuccess.Should().BeFalse();
        log.ErrorMessage.Should().Contain("Bağlantı");
        log.ErpReferenceNumber.Should().BeNull();
        log.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void MarkRetried_Success_ShouldIncrementAttemptCount()
    {
        var log = ErpSyncLog.CreateFailure(
            _tenantId, ErpProvider.Logo, "Order", _entityId, "Hata", attempt: 1);

        log.MarkRetried(true, "LOGO-REF-002", null);

        log.IsSuccess.Should().BeTrue();
        log.AttemptCount.Should().Be(2);
        log.ErpReferenceNumber.Should().Be("LOGO-REF-002");
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CreateSuccess_EntityType_ShouldBePreserved()
    {
        var log = ErpSyncLog.CreateSuccess(
            _tenantId, ErpProvider.Logo, "Invoice", _entityId, "INV-001");

        log.EntityType.Should().Be("Invoice");
        log.EntityId.Should().Be(_entityId);
    }
}
