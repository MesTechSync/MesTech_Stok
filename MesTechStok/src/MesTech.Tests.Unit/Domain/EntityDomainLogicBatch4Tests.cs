using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 9: Entity domain logic batch 4
// FeedImportLog, Milestone, PipelineStage, LoyaltyProgram,
// ImportTemplate, ShipmentCost, NotificationTemplate, PersonalDataRetentionPolicy
// Target: entity coverage %38 → %50
// ════════════════════════════════════════════════════════

#region FeedImportLog — Import Lifecycle

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class FeedImportLogDomainTests
{
    [Fact]
    public void Constructor_ValidParams_StartsInProgress()
    {
        var log = new FeedImportLog(Guid.NewGuid(), Guid.NewGuid());
        log.Status.Should().Be(FeedSyncStatus.InProgress);
        log.CompletedAt.Should().BeNull();
        log.Duration.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyFeedId_Throws()
    {
        var act = () => new FeedImportLog(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*SupplierFeedId*");
    }

    [Fact]
    public void Complete_SetsCompletedStatus()
    {
        var log = new FeedImportLog(Guid.NewGuid(), Guid.NewGuid());
        log.Complete(100, 20, 70, 10);

        log.Status.Should().Be(FeedSyncStatus.Completed);
        log.TotalProducts.Should().Be(100);
        log.NewProducts.Should().Be(20);
        log.UpdatedProducts.Should().Be(70);
        log.DeactivatedProducts.Should().Be(10);
        log.CompletedAt.Should().NotBeNull();
        log.Duration.Should().NotBeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CompletePartially_SetsPartialStatus()
    {
        var log = new FeedImportLog(Guid.NewGuid(), Guid.NewGuid());
        log.CompletePartially(50, 10, 30, 5, "3 ürün parse edilemedi");

        log.Status.Should().Be(FeedSyncStatus.PartiallyCompleted);
        log.ErrorMessage.Should().Contain("parse edilemedi");
    }

    [Fact]
    public void Fail_SetsFailedStatus()
    {
        var log = new FeedImportLog(Guid.NewGuid(), Guid.NewGuid());
        log.Fail("Network timeout");

        log.Status.Should().Be(FeedSyncStatus.Failed);
        log.ErrorMessage.Should().Be("Network timeout");
        log.CompletedAt.Should().NotBeNull();
    }
}

#endregion

#region Milestone — Project Tracking

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class MilestoneDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var milestone = Milestone.Create(Guid.NewGuid(), Guid.NewGuid(), "MVP Release", 1, DateTime.UtcNow.AddDays(30));
        milestone.Name.Should().Be("MVP Release");
        milestone.Position.Should().Be(1);
        milestone.Status.Should().Be(MilestoneStatus.Pending);
    }

    [Fact]
    public void MarkDone_SetsStatus()
    {
        var milestone = Milestone.Create(Guid.NewGuid(), Guid.NewGuid(), "Alpha", 1);
        milestone.MarkDone();
        milestone.Status.Should().Be(MilestoneStatus.Done);
    }

    [Fact]
    public void MarkOverdue_SetsStatus()
    {
        var milestone = Milestone.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", 2);
        milestone.MarkOverdue();
        milestone.Status.Should().Be(MilestoneStatus.Overdue);
    }
}

#endregion

#region PipelineStage — CRM Pipeline

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class PipelineStageDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var stage = PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Teklif", 1, 30m, StageType.Normal);
        stage.Name.Should().Be("Teklif");
        stage.Position.Should().Be(1);
    }

    [Fact]
    public void Create_ProbabilityAbove100_Throws()
    {
        var act = () => PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", 1, 110m, StageType.Normal);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeProbability_Throws()
    {
        var act = () => PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", 1, -5m, StageType.Normal);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdatePosition_ChangesPosition()
    {
        var stage = PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Teklif", 1, 30m, StageType.Normal);
        stage.UpdatePosition(5);
        stage.Position.Should().Be(5);
    }
}

#endregion

#region LoyaltyProgram

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class LoyaltyProgramDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var program = LoyaltyProgram.Create(Guid.NewGuid(), "VIP Puan", 1.5m, 100);
        program.Name.Should().Be("VIP Puan");
        program.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var program = LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 50);
        program.Deactivate();
        program.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateRules_ChangesValues()
    {
        var program = LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 50);
        program.UpdateRules(2.5m, 200);
        program.PointsPerPurchase.Should().Be(2.5m);
        program.MinRedeemPoints.Should().Be(200);
    }
}

#endregion

#region ImportTemplate

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class ImportTemplateDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var template = ImportTemplate.Create(Guid.NewGuid(), "Trendyol Import", "CSV");
        template.Name.Should().Be("Trendyol Import");
        template.Format.Should().Be("CSV");
    }

    [Fact]
    public void Create_EmptyTenant_Throws()
    {
        var act = () => ImportTemplate.Create(Guid.Empty, "Test", "CSV");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMapping_IncreasesFieldCount()
    {
        var template = ImportTemplate.Create(Guid.NewGuid(), "Test", "CSV");
        template.AddMapping("urun_adi", "Name");
        template.AddMapping("fiyat", "Price");
        template.FieldCount.Should().Be(2);
    }

    [Fact]
    public void MarkUsed_UpdatesLastUsedAt()
    {
        var template = ImportTemplate.Create(Guid.NewGuid(), "Test", "CSV");
        template.MarkUsed();
        template.LastUsedAt.Should().NotBeNull();
    }
}

#endregion

#region ShipmentCost

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class ShipmentCostDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var cost = ShipmentCost.Create(Guid.NewGuid(), Guid.NewGuid(), CargoProvider.Yurtici, 45.90m);
        cost.Cost.Should().Be(45.90m);
    }

    [Fact]
    public void Create_NegativeCost_Throws()
    {
        var act = () => ShipmentCost.Create(Guid.NewGuid(), Guid.NewGuid(), CargoProvider.Yurtici, -10m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroCost_Succeeds()
    {
        var cost = ShipmentCost.Create(Guid.NewGuid(), Guid.NewGuid(), CargoProvider.Yurtici, 0m);
        cost.Cost.Should().Be(0m);
    }
}

#endregion

#region NotificationTemplate

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class NotificationTemplateDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var template = NotificationTemplate.Create(Guid.NewGuid(), "Sipariş Onayı",
            "Siparişiniz #{orderId} onaylandı", NotificationChannel.Email);
        template.Name.Should().Be("Sipariş Onayı");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var template = NotificationTemplate.Create(Guid.NewGuid(), "Test", "body", NotificationChannel.Email);
        template.Deactivate();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var template = NotificationTemplate.Create(Guid.NewGuid(), "Test", "body", NotificationChannel.Email);
        template.Deactivate();
        template.Activate();
        template.IsActive.Should().BeTrue();
    }
}

#endregion

#region PersonalDataRetentionPolicy — KVKK

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class PersonalDataRetentionPolicyDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var policy = PersonalDataRetentionPolicy.Create(Guid.NewGuid(), "Müşteri Verileri", 365);
        policy.DataCategory.Should().Be("Müşteri Verileri");
        policy.RetentionDays.Should().Be(365);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateRetention_ChangesValues()
    {
        var policy = PersonalDataRetentionPolicy.Create(Guid.NewGuid(), "Test", 180);
        policy.UpdateRetention(730, "KVKK 2 yıl zorunluluğu");
        policy.RetentionDays.Should().Be(730);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var policy = PersonalDataRetentionPolicy.Create(Guid.NewGuid(), "Test", 180);
        policy.Deactivate();
        policy.IsActive.Should().BeFalse();
    }
}

#endregion
