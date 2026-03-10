using MesTech.Domain.Common;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Tests.Unit.Domain;

public class EventRecordTests
{
    [Fact]
    public void DailySummaryGeneratedEvent_ImplementsIDomainEvent()
    {
        var evt = new DailySummaryGeneratedEvent(
            DateTime.Today, 15, 12500.50m, 3, 10, DateTime.UtcNow);
        Assert.IsAssignableFrom<IDomainEvent>(evt);
        Assert.Equal(15, evt.OrderCount);
        Assert.Equal(12500.50m, evt.Revenue);
    }

    [Fact]
    public void SyncErrorOccurredEvent_ImplementsIDomainEvent()
    {
        var evt = new SyncErrorOccurredEvent(
            "Trendyol", "HttpTimeout", "Connection timed out", DateTime.UtcNow);
        Assert.IsAssignableFrom<IDomainEvent>(evt);
        Assert.Equal("Trendyol", evt.Platform);
        Assert.Equal("HttpTimeout", evt.ErrorType);
    }
}
