using FluentAssertions;
using MesTech.Infrastructure.Messaging.Mesa;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MesaEventMonitor gercek implementasyon unit testleri — DEV 5 Dalga 3 Hafta 12.
/// Mock kullanilmaz; gercek MesaEventMonitor instance test edilir.
/// Thread-safety ve sayac dogruluklari icin tasarlanmistir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga3")]
public class MesaEventMonitorTests
{
    [Fact]
    public void RecordPublish_IncrementPublishedCount()
    {
        var monitor = new MesaEventMonitor();

        monitor.RecordPublish("product.created");

        var status = monitor.GetStatus();
        status.Events["product.created"].Published.Should().Be(1);
    }

    [Fact]
    public void RecordConsume_IncrementConsumedCount()
    {
        var monitor = new MesaEventMonitor();

        monitor.RecordConsume("ai.content.generated");

        var status = monitor.GetStatus();
        status.Events["ai.content.generated"].Consumed.Should().Be(1);
    }

    [Fact]
    public void RecordError_IncrementErrorCount()
    {
        var monitor = new MesaEventMonitor();

        monitor.RecordError("product.created", "Something went wrong");

        var status = monitor.GetStatus();
        status.Events["product.created"].Errors.Should().Be(1);
    }

    [Fact]
    public void RecordPublish_MultipleEventTypes_TrackedSeparately()
    {
        var monitor = new MesaEventMonitor();

        monitor.RecordPublish("event.a");
        monitor.RecordPublish("event.a");
        monitor.RecordPublish("event.b");

        var status = monitor.GetStatus();
        status.Events["event.a"].Published.Should().Be(2);
        status.Events["event.b"].Published.Should().Be(1);
    }

    [Fact]
    public void GetStatus_NoActivity_ReturnsEmptyEvents()
    {
        var monitor = new MesaEventMonitor();

        var status = monitor.GetStatus();

        status.Events.Should().BeEmpty();
    }

    [Fact]
    public void GetStatus_ReturnsActiveStatus()
    {
        var monitor = new MesaEventMonitor();

        var status = monitor.GetStatus();

        status.BridgeStatus.Should().Be("active");
    }

    [Fact]
    public void RecordPublish_MultipleTimes_AccumulatesCount()
    {
        var monitor = new MesaEventMonitor();

        monitor.RecordPublish("order.received");
        monitor.RecordPublish("order.received");
        monitor.RecordPublish("order.received");

        var status = monitor.GetStatus();
        status.Events["order.received"].Published.Should().Be(3);
    }

    [Fact]
    public void ThreadSafety_ConcurrentRecords_NoException()
    {
        var monitor = new MesaEventMonitor();

        var act = () => Parallel.For(0, 100, _ =>
        {
            monitor.RecordPublish("concurrent.event");
            monitor.RecordConsume("concurrent.event");
            monitor.RecordError("concurrent.event", "parallel error");
        });

        act.Should().NotThrow();

        var status = monitor.GetStatus();
        status.Events["concurrent.event"].Published.Should().Be(100);
        status.Events["concurrent.event"].Consumed.Should().Be(100);
        status.Events["concurrent.event"].Errors.Should().Be(100);
    }
}
