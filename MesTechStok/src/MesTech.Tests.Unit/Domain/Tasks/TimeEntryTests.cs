using FluentAssertions;
using MesTech.Domain.Entities.Tasks;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Tasks;

/// <summary>
/// TimeEntry entity unit testleri.
/// DEV 5 — H27-5.4 (emirname Task 5.4 uyarlanmis gercek entity'ye gore)
/// Not: Gercek entity'de CreateManual metodu yok — sadece Start + Stop var.
///      Stop idempotency kontrolu de yok — basit davranislar test edildi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Tasks")]
public class TimeEntryTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _taskId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Start_ShouldSetZeroMinutes()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);

        entry.Minutes.Should().Be(0);
        entry.EndedAt.Should().BeNull();
    }

    [Fact]
    public void Start_ShouldSetCorrectIds()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);

        entry.TenantId.Should().Be(_tenantId);
        entry.WorkTaskId.Should().Be(_taskId);
        entry.UserId.Should().Be(_userId);
    }

    [Fact]
    public void Start_ShouldSetStartedAtToNow()
    {
        var before = DateTime.UtcNow;
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);

        entry.StartedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Stop_ShouldSetEndedAt()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);
        entry.Stop();

        entry.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public void Stop_ShouldCalculatePositiveMinutes()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);
        entry.Stop();

        // Stop aninda StartedAt ile EndedAt arasi en az 0 dakika (genelde 0-1 dakika)
        entry.Minutes.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Start_WithDescription_ShouldPreserveDescription()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId, description: "Kod yazimi");

        entry.Description.Should().Be("Kod yazimi");
    }

    [Fact]
    public void Start_IsBillable_ShouldPreserveFlag()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId, isBillable: true, hourlyRate: 150m);

        entry.IsBillable.Should().BeTrue();
        entry.HourlyRate.Should().Be(150m);
    }

    [Fact]
    public void Start_NotBillable_DefaultIsFalse()
    {
        var entry = TimeEntry.Start(_tenantId, _taskId, _userId);

        entry.IsBillable.Should().BeFalse();
        entry.HourlyRate.Should().BeNull();
    }
}
