using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Crm;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Crm;

public class DealTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _pipelineId = Guid.NewGuid();
    private static readonly Guid _stageId = Guid.NewGuid();

    private static Deal CreateDeal(decimal amount = 5000m) =>
        Deal.Create(_tenantId, "Test Fırsatı", _pipelineId, _stageId, amount);

    // ── CREATE ──────────────────────────────────────────────────────
    [Fact]
    public void Create_WithValidData_ShouldSetStatusToOpen()
    {
        var deal = CreateDeal();
        deal.Status.Should().Be(DealStatus.Open);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => Deal.Create(_tenantId, "Bad Deal", _pipelineId, _stageId, -100m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => Deal.Create(_tenantId, "", _pipelineId, _stageId, 1000m);
        act.Should().Throw<ArgumentException>();
    }

    // ── MOVE STAGE ───────────────────────────────────────────────────
    [Fact]
    public void MoveToStage_OpenDeal_ShouldUpdateStageId()
    {
        var deal = CreateDeal();
        var newStageId = Guid.NewGuid();
        deal.MoveToStage(newStageId);
        deal.StageId.Should().Be(newStageId);
    }

    [Fact]
    public void MoveToStage_ShouldRaiseDealStageChangedEvent()
    {
        var deal = CreateDeal();
        deal.MoveToStage(Guid.NewGuid());
        deal.DomainEvents.Should().ContainSingle(e => e is DealStageChangedEvent);
    }

    [Fact]
    public void MoveToStage_ClosedDeal_ShouldThrow()
    {
        var deal = CreateDeal();
        deal.MarkAsWon();
        var act = () => deal.MoveToStage(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    // ── WIN ──────────────────────────────────────────────────────────
    [Fact]
    public void MarkAsWon_OpenDeal_ShouldSetStatusToWon()
    {
        var deal = CreateDeal();
        deal.MarkAsWon();
        deal.Status.Should().Be(DealStatus.Won);
    }

    [Fact]
    public void MarkAsWon_WithOrderId_ShouldLinkOrder()
    {
        var deal = CreateDeal();
        var orderId = Guid.NewGuid();
        deal.MarkAsWon(orderId);
        deal.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void MarkAsWon_ShouldRaiseDealWonEvent()
    {
        var deal = CreateDeal(10000m);
        deal.MarkAsWon();
        deal.DomainEvents.OfType<DealWonEvent>().Single().Amount.Should().Be(10000m);
    }

    [Fact]
    public void MarkAsWon_AlreadyClosed_ShouldThrow()
    {
        var deal = CreateDeal();
        deal.MarkAsWon();
        var act = () => deal.MarkAsWon();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── LOST ─────────────────────────────────────────────────────────
    [Fact]
    public void MarkAsLost_WithReason_ShouldSetStatusToLost()
    {
        var deal = CreateDeal();
        deal.MarkAsLost("Bütçe yok.");
        deal.Status.Should().Be(DealStatus.Lost);
        deal.LostReason.Should().Be("Bütçe yok.");
    }

    [Fact]
    public void MarkAsLost_ShouldRaiseDealLostEvent()
    {
        var deal = CreateDeal();
        deal.MarkAsLost("Rakip kazandı.");
        deal.DomainEvents.Should().ContainSingle(e => e is DealLostEvent);
    }

    [Fact]
    public void MarkAsLost_EmptyReason_ShouldThrow()
    {
        var deal = CreateDeal();
        var act = () => deal.MarkAsLost("");
        act.Should().Throw<ArgumentException>();
    }
}
