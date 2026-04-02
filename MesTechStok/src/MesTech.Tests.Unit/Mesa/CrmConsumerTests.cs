using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Consumers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// 2 CRM Consumer unit tests: MesaLeadScoredConsumer + MesaMeetingScheduledConsumer.
/// DEV5: Coverage lift 24% -> 80%+.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class CrmConsumerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> Monitor() => new();
    private static Mock<IMediator> Mediator() => new();
    private static Mock<IUnitOfWork> UnitOfWork() => new();
    private static ILogger<T> Logger<T>() => new Mock<ILogger<T>>().Object;

    // ══════════════════════════════════════════════
    //  MesaLeadScoredConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task LeadScored_Consume_LeadNotFound_DoesNotThrow()
    {
        var monitor = Monitor();
        var leadRepo = new Mock<ICrmLeadRepository>();
        var uow = UnitOfWork();

        leadRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Lead));

        var consumer = new MesaLeadScoredConsumer(
            monitor.Object, leadRepo.Object, uow.Object,
            Logger<MesaLeadScoredConsumer>());

        var ctx = new Mock<ConsumeContext<MesaLeadScoredEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaLeadScoredEvent(
            Guid.NewGuid(), 85, "Buyuk sirket, yuksek butce",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        // Lead not found — consumer should return gracefully (no exception)
        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LeadScored_Consume_LeadNotFound_DoesNotRecordConsume()
    {
        var monitor = Monitor();
        var leadRepo = new Mock<ICrmLeadRepository>();
        var uow = UnitOfWork();

        leadRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Lead));

        var consumer = new MesaLeadScoredConsumer(
            monitor.Object, leadRepo.Object, uow.Object,
            Logger<MesaLeadScoredConsumer>());

        var ctx = new Mock<ConsumeContext<MesaLeadScoredEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaLeadScoredEvent(
            Guid.NewGuid(), 50, "Orta potansiyel",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        // Lead not found — RecordConsume should NOT be called
        monitor.Verify(m => m.RecordConsume(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LeadScored_Consume_LeadFound_RecordsConsume()
    {
        var monitor = Monitor();
        var leadRepo = new Mock<ICrmLeadRepository>();
        var uow = UnitOfWork();

        var leadId = Guid.NewGuid();
        var lead = Lead.Create(TestTenantId, "Test User", LeadSource.Web, "test@test.com");
        leadRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var consumer = new MesaLeadScoredConsumer(
            monitor.Object, leadRepo.Object, uow.Object,
            Logger<MesaLeadScoredConsumer>());

        var ctx = new Mock<ConsumeContext<MesaLeadScoredEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaLeadScoredEvent(
            leadId, 92, "Premium musteri, yuksek butce",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume(nameof(MesaLeadScoredEvent)), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  MesaMeetingScheduledConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task MeetingScheduled_Consume_CallsMediatorSend()
    {
        var mediator = Mediator();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var consumer = new MesaMeetingScheduledConsumer(
            mediator.Object, Logger<MesaMeetingScheduledConsumer>());

        var ctx = new Mock<ConsumeContext<MesaMeetingScheduledEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaMeetingScheduledEvent(
            "Sprint Planning", DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3),
            "Zoom Meeting", new List<Guid> { Guid.NewGuid() },
            TestTenantId, null, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        mediator.Verify(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MeetingScheduled_Consume_ValidEvent_DoesNotThrow()
    {
        var mediator = Mediator();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var consumer = new MesaMeetingScheduledConsumer(
            mediator.Object, Logger<MesaMeetingScheduledConsumer>());

        var ctx = new Mock<ConsumeContext<MesaMeetingScheduledEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaMeetingScheduledEvent(
            "Daily Standup", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(15),
            null, new List<Guid>(), TestTenantId, Guid.NewGuid(), DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MeetingScheduled_Consume_MediatorThrows_RethrowsForRetry()
    {
        var mediator = Mediator();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Calendar conflict"));

        var consumer = new MesaMeetingScheduledConsumer(
            mediator.Object, Logger<MesaMeetingScheduledConsumer>());

        var ctx = new Mock<ConsumeContext<MesaMeetingScheduledEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaMeetingScheduledEvent(
            "Conflict Meeting", DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
            null, new List<Guid>(), TestTenantId, null, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
